using Microsoft.AspNetCore.Mvc.Testing;
using WireMock.Server;

namespace Gateway.IntegrationTests;

public class GatewayApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // One fake downstream stands in for every service; each route is told apart by its path.
    public WireMockServer Downstream { get; private set; } = null!;

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
