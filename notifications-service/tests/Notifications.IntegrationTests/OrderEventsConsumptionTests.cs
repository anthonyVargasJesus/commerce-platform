using System.Net;
using System.Net.Http.Json;
using Commerce.Contracts.Inventory;
using Commerce.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Notifications.Dtos;
using Notifications.Domain.Notifications;
using Shouldly;

namespace Notifications.IntegrationTests;

public class OrderEventsConsumptionTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    private sealed record NotificationsPage(IReadOnlyList<NotificationDto> Items);

    private sealed record MailpitMessage(string Subject, string Snippet);

    private sealed record MailpitSearch(IReadOnlyList<MailpitMessage> Messages);

    private readonly HttpClient _client = factory.CreateClient();

    private async Task PublishAsync<T>(T message, Guid? messageId = null)
        where T : class
    {
        using var scope = factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publisher.Publish(message, context =>
        {
            if (messageId is not null)
            {
                context.MessageId = messageId;
            }
        });
    }

    private async Task<IReadOnlyList<NotificationDto>> WaitForNotificationsAsync(Guid id, int expectedCount, string filter = "orderId")
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        IReadOnlyList<NotificationDto> items = [];

        while (DateTime.UtcNow < deadline)
        {
            var page = await _client.GetFromJsonAsync<NotificationsPage>($"/api/v1/notifications?{filter}={id}");
            items = page!.Items;
            if (items.Count >= expectedCount)
            {
                break;
            }

            await Task.Delay(250);
        }

        return items;
    }

    [Fact]
    public async Task OrderConfirmed_WhenPublished_ShouldCreateAConfirmationNotification()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await PublishAsync(new OrderConfirmed(orderId, customerId, "Jane Doe", "jane@example.com", 120m, DateTimeOffset.UtcNow));

        var items = await WaitForNotificationsAsync(orderId, 1);

        var notification = items.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.OrderConfirmed);
        notification.CustomerId.ShouldBe(customerId);
        notification.Recipient.ShouldBe("jane@example.com");
        notification.Message.ShouldContain(orderId.ToString());
    }

    private async Task<MailpitMessage> WaitForEmailAsync(string query)
    {
        using var mailpit = new HttpClient { BaseAddress = new Uri(factory.MailpitApiUrl) };
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            var result = await mailpit.GetFromJsonAsync<MailpitSearch>($"/api/v1/search?query={Uri.EscapeDataString(query)}");
            if (result!.Messages.Count > 0)
            {
                return result.Messages[0];
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"No email matching '{query}' arrived in Mailpit.");
    }

    [Fact]
    public async Task OrderConfirmed_WhenPublished_ShouldEmailTheCustomer()
    {
        var orderId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@example.com";

        await PublishAsync(new OrderConfirmed(orderId, Guid.NewGuid(), "Jane Doe", email, 75m, DateTimeOffset.UtcNow));

        var mail = await WaitForEmailAsync($"to:{email}");

        mail.Subject.ShouldBe("Your order is confirmed");
        mail.Snippet.ShouldContain("Jane Doe");
        mail.Snippet.ShouldContain(orderId.ToString());
    }

    [Fact]
    public async Task ProductLowStock_WhenPublished_ShouldEmailTheOperationsAddress()
    {
        var name = $"Widget-{Guid.NewGuid():N}";

        await PublishAsync(new ProductLowStock(Guid.NewGuid(), "SKU-8", name, 1, 5, DateTimeOffset.UtcNow));

        var mail = await WaitForEmailAsync($"to:ops@commerce-platform.local {name}");

        mail.Subject.ShouldBe("Low stock alert");
        mail.Snippet.ShouldContain(name);
    }

    [Fact]
    public async Task SameMessageDeliveredTwice_ShouldCreateOnlyOneNotification()
    {
        var orderId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var message = new OrderShipped(orderId, Guid.NewGuid(), "Jane Doe", "jane@example.com", 10m, DateTimeOffset.UtcNow);

        await PublishAsync(message, messageId);
        await PublishAsync(message, messageId);

        await WaitForNotificationsAsync(orderId, 1);
        await Task.Delay(TimeSpan.FromSeconds(3));
        var items = await WaitForNotificationsAsync(orderId, 1);

        items.ShouldHaveSingleItem().Type.ShouldBe(NotificationType.OrderShipped);
    }

    [Fact]
    public async Task ProductLowStock_WhenPublished_ShouldCreateAStockAlertForTheProduct()
    {
        var productId = Guid.NewGuid();

        await PublishAsync(new ProductLowStock(productId, "SKU-7", "Widget", 2, 5, DateTimeOffset.UtcNow));

        var items = await WaitForNotificationsAsync(productId, 1, "productId");

        var alert = items.ShouldHaveSingleItem();
        alert.Type.ShouldBe(NotificationType.LowStock);
        alert.OrderId.ShouldBeNull();
        alert.CustomerId.ShouldBeNull();
        alert.Message.ShouldContain("Widget");
    }

    [Fact]
    public async Task GetById_WhenNotificationDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/notifications/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetList_WithInvalidPageSize_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/v1/notifications?pageSize=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
