using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Orders.IntegrationTests;

public class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder("rabbitmq:3.13-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public WireMockServer InventoryServer { get; private set; } = null!;

    public HttpClient CreateClientFor(string email, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = TestTokens.BearerFor(email, roles);
        return client;
    }

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
            var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            dbContext.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dbContainer.StartAsync(), _rabbitContainer.StartAsync());

        InventoryServer = WireMockServer.Start();

        // The same fake server also plays Keycloak's token endpoint for orders-service's client credentials.
        InventoryServer
            .Given(Request.Create().WithPath("/token").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { access_token = "service-token", expires_in = 300 }));

        // Program.cs reads ConnectionStrings/Services config eagerly while building the host,
        // before WebApplicationFactory's ConfigureAppConfiguration overrides take effect —
        // env vars are read earlier (at builder-creation time), so they actually apply.
        // (Same root cause documented in inventory-service/tests/.../InventoryApiFactory.cs.)
        Environment.SetEnvironmentVariable("ConnectionStrings__OrdersDb", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("Services__Inventory__BaseUrl", InventoryServer.Url);
        Environment.SetEnvironmentVariable("ServiceAuth__TokenEndpoint", InventoryServer.Url + "/token");
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitContainer.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitContainer.GetMappedPublicPort(5672).ToString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        InventoryServer.Stop();
        await _dbContainer.StopAsync();
        await _rabbitContainer.StopAsync();
        await base.DisposeAsync();
    }
}
