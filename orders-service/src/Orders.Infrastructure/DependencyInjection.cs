using MassTransit;
using Orders.Application.Common.Interfaces;
using Orders.Infrastructure.Messaging;
using Orders.Infrastructure.ExternalServices.Inventory;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;
using Orders.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrdersDb")
            ?? throw new InvalidOperationException("Connection string 'OrdersDb' was not found.");

        services.AddScoped<DomainEventsOutboxInterceptor>();

        services.AddDbContext<OrdersDbContext>((serviceProvider, options) => options
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders"))
            .AddInterceptors(serviceProvider.GetRequiredService<DomainEventsOutboxInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        var inventoryBaseUrl = configuration["Services:Inventory:BaseUrl"]
            ?? throw new InvalidOperationException("Configuration 'Services:Inventory:BaseUrl' was not found.");

        var serviceAuth = new ServiceTokenOptions(
            configuration["ServiceAuth:TokenEndpoint"] ?? throw new InvalidOperationException("Configuration 'ServiceAuth:TokenEndpoint' was not found."),
            configuration["ServiceAuth:ClientId"] ?? throw new InvalidOperationException("Configuration 'ServiceAuth:ClientId' was not found."),
            configuration["ServiceAuth:ClientSecret"] ?? throw new InvalidOperationException("Configuration 'ServiceAuth:ClientSecret' was not found."));

        services.AddSingleton(serviceAuth);
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient(ServiceTokenProvider.HttpClientName);
        services.AddSingleton<ServiceTokenProvider>();
        services.AddTransient<ServiceTokenHandler>();

        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
            {
                client.BaseAddress = new Uri(inventoryBaseUrl);
            })
            .AddHttpMessageHandler<ServiceTokenHandler>()
            .AddStandardResilienceHandler();

        var rabbitHost = configuration["RabbitMq:Host"]
            ?? throw new InvalidOperationException("Configuration RabbitMq:Host was not found.");
        var rabbitPort = configuration.GetValue("RabbitMq:Port", 5672);
        var rabbitUser = configuration["RabbitMq:Username"] ?? "guest";
        var rabbitPassword = configuration["RabbitMq:Password"] ?? "guest";

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<OrdersDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, (ushort)rabbitPort, "/", host =>
                {
                    host.Username(rabbitUser);
                    host.Password(rabbitPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
