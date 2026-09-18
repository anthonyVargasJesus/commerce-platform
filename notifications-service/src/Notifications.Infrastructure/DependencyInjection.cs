using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Common.Interfaces;
using Notifications.Infrastructure.Notifications;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationsDb")
            ?? throw new InvalidOperationException("Connection string 'NotificationsDb' was not found.");

        services.AddDbContext<NotificationsDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "notifications")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();

        var rabbitHost = configuration["RabbitMq:Host"]
            ?? throw new InvalidOperationException("Configuration 'RabbitMq:Host' was not found.");
        var rabbitPort = configuration.GetValue("RabbitMq:Port", 5672);
        var rabbitUser = configuration["RabbitMq:Username"] ?? "guest";
        var rabbitPassword = configuration["RabbitMq:Password"] ?? "guest";

        services.AddMassTransit(bus =>
        {
            bus.AddConsumers(typeof(DependencyInjection).Assembly);

            // The inbox makes consumption idempotent: RabbitMQ delivers at least once, so a redelivered
            // message is detected by its MessageId and skipped instead of creating a duplicate notification.
            bus.AddEntityFrameworkOutbox<NotificationsDbContext>(outbox => outbox.UsePostgres());
            bus.AddConfigureEndpointsCallback((context, _, endpoint) => endpoint.UseEntityFrameworkOutbox<NotificationsDbContext>(context));

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
