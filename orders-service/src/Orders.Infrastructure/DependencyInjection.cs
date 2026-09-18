using Orders.Application.Common.Interfaces;
using Orders.Infrastructure.ExternalServices.Inventory;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;
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

        services.AddDbContext<OrdersDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "orders")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        var inventoryBaseUrl = configuration["Services:Inventory:BaseUrl"]
            ?? throw new InvalidOperationException("Configuration 'Services:Inventory:BaseUrl' was not found.");

        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
            {
                client.BaseAddress = new Uri(inventoryBaseUrl);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
