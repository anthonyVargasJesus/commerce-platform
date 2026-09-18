using System.Threading.RateLimiting;
using Gateway.API.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Gateway.API")
    .WriteTo.OpenTelemetry(options => options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = "Gateway.API" }));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddAuthorizationBuilder().AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());

// Browsers may only call the API from the origins listed in Cors:AllowedOrigins.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location", "Retry-After")));

// Each user (the "sub" of the token, or the client IP when there is no token) gets its own sliding window.
// The limits are read on each request so they can be changed without a restart.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        // A sliding window does not say when a permit frees up; after one full window every earlier request has
        // expired, so the window length is a safe value to tell the client.
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : context.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimiting:WindowSeconds", 60);
        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return ValueTask.CompletedTask;
    };

    options.AddPolicy("per-user", context =>
    {
        var settings = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("RateLimiting");
        var key = context.User.FindFirst("sub")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = settings.GetValue("PermitLimit", 600),
            Window = TimeSpan.FromSeconds(settings.GetValue("WindowSeconds", 60)),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
        });
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Gateway.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

app.UseSerilogRequestLogging();

// Order matters: CORS answers preflight requests before authentication; the limiter runs after authentication
// (it needs the user) but before authorization, so unauthenticated floods are limited too.
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health/live");

app.MapReverseProxy();

await app.RunAsync();
