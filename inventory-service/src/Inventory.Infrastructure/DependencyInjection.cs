using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Messaging;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException("Connection string 'InventoryDb' was not found.");

        services.AddScoped<DomainEventsOutboxInterceptor>();

        services.AddDbContext<InventoryDbContext>((serviceProvider, options) => options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "inventory"))
            .AddInterceptors(serviceProvider.GetRequiredService<DomainEventsOutboxInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductTypeRepository, ProductTypeRepository>();

        var rabbitHost = configuration["RabbitMq:Host"]
            ?? throw new InvalidOperationException("Configuration RabbitMq:Host was not found.");
        var rabbitPort = configuration.GetValue("RabbitMq:Port", 5672);
        var rabbitUser = configuration["RabbitMq:Username"] ?? "guest";
        var rabbitPassword = configuration["RabbitMq:Password"] ?? "guest";

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<InventoryDbContext>(outbox =>
            {
                outbox.UsePostgres();
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
