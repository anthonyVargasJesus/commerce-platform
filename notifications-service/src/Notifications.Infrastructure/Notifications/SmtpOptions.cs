namespace Notifications.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 1025;

    public string From { get; init; } = "no-reply@commerce-platform.local";

    // Internal alerts (e.g. low stock) have no customer, so they go to this address.
    public string OperationsEmail { get; init; } = "ops@commerce-platform.local";
}
