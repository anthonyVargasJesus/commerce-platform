using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Notifications.IntegrationTests;

public class NotificationsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder("rabbitmq:3.13-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    // Mailpit is an SMTP server that captures every message and exposes them over a REST API.
    private readonly IContainer _mailpitContainer = new ContainerBuilder("axllent/mailpit:latest")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8025).ForPath("/livez")))
        .Build();

    public string MailpitApiUrl => $"http://{_mailpitContainer.Hostname}:{_mailpitContainer.GetMappedPublicPort(8025)}";

    public HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = TestTokens.Bearer(roles);
        return client;
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Real JWT validation (issuer, audience, expiry, role mapping) with a test signing key instead of Keycloak's.
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration());
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.SigningKey;
            }));

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            dbContext.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dbContainer.StartAsync(), _rabbitContainer.StartAsync(), _mailpitContainer.StartAsync());

        // Program.cs reads ConnectionStrings/RabbitMq config eagerly while building the host, before
        // WebApplicationFactory's ConfigureAppConfiguration overrides take effect — env vars are read
        // earlier (at builder-creation time), so they actually apply.
        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("Smtp__Host", _mailpitContainer.Hostname);
        Environment.SetEnvironmentVariable("Smtp__Port", _mailpitContainer.GetMappedPublicPort(1025).ToString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitContainer.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitContainer.GetMappedPublicPort(5672).ToString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _rabbitContainer.StopAsync();
        await _mailpitContainer.StopAsync();
        await base.DisposeAsync();
    }
}
