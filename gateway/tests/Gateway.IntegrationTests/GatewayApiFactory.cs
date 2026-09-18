using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using WireMock.Server;

namespace Gateway.IntegrationTests;

public class GatewayApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // One fake downstream stands in for every service; each route is told apart by its path.
    public WireMockServer Downstream { get; private set; } = null!;

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

    }

    public Task InitializeAsync()
    {
        Downstream = WireMockServer.Start();

        // Program.cs reads the ReverseProxy config while building the host, before
        // WebApplicationFactory's ConfigureAppConfiguration overrides take effect — env vars are read
        // earlier (at builder-creation time), so they actually apply.
        foreach (var cluster in new[] { "inventory", "orders", "notifications" })
        {
            Environment.SetEnvironmentVariable($"ReverseProxy__Clusters__{cluster}__Destinations__primary__Address", Downstream.Url + "/");
        }

        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Downstream.Stop();
        await base.DisposeAsync();
    }
}
