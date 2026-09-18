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

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            dbContext.Database.Migrate();
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dbContainer.StartAsync(), _rabbitContainer.StartAsync());

        // Program.cs reads ConnectionStrings/RabbitMq config eagerly while building the host, before
        // WebApplicationFactory's ConfigureAppConfiguration overrides take effect — env vars are read
        // earlier (at builder-creation time), so they actually apply.
        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb", _dbContainer.GetConnectionString());
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
