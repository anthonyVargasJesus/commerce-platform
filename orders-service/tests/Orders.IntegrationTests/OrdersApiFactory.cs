using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using WireMock.Server;

namespace Orders.IntegrationTests;

public class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder("rabbitmq:3.13-management").Build();

    public WireMockServer InventoryServer { get; private set; } = null!;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
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

        // Program.cs reads ConnectionStrings/Services config eagerly while building the host,
        // before WebApplicationFactory's ConfigureAppConfiguration overrides take effect —
        // env vars are read earlier (at builder-creation time), so they actually apply.
        // (Same root cause documented in inventory-service/tests/.../InventoryApiFactory.cs.)
        Environment.SetEnvironmentVariable("ConnectionStrings__OrdersDb", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("Services__Inventory__BaseUrl", InventoryServer.Url);
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
