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
        // Channel "Smtp" (default) sends real emails; "Log" only writes to the log.
        if (string.Equals(configuration["Notifications:Channel"], "Log", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<INotificationSender, LoggingNotificationSender>();
        }
        else
        {
            services.AddSingleton(configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new SmtpOptions());
            services.AddSingleton<INotificationSender, SmtpNotificationSender>();
        }

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
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                // Retry wraps the inbox/outbox so every attempt runs in a fresh transaction; a message that
                // keeps failing (e.g. SMTP down) ends up in the _error queue instead of being lost.
                endpoint.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(2)));
                endpoint.UseEntityFrameworkOutbox<NotificationsDbContext>(context);
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
