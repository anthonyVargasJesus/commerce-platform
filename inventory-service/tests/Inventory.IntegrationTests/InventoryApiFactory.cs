using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Inventory.IntegrationTests;

public class InventoryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("inventory_test")
        .WithUsername("inventory")
        .WithPassword("inventory")
        .Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder("rabbitmq:3.13-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            dbContext.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dbContainer.StartAsync(), _rabbitContainer.StartAsync());

        // Program.cs reads ConnectionStrings:InventoryDb eagerly while building the host, before
        // WebApplicationFactory applies ConfigureAppConfiguration overrides — so an in-memory config
        // override arrives too late. Environment variables are read earlier, at builder-creation time,
        // so setting one here (before the host is built) is what actually takes effect.
        Environment.SetEnvironmentVariable("ConnectionStrings__InventoryDb", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitContainer.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitContainer.GetMappedPublicPort(5672).ToString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _rabbitContainer.StopAsync();
        await base.DisposeAsync();
    }
}
