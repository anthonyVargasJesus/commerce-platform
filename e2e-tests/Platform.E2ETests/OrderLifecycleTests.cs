using System.Net;
using Shouldly;

namespace Platform.E2ETests;

[Collection(PlatformCollection.Name)]
public class OrderLifecycleTests
{
    private const string MariaEmail = "maria@example.com";

    [Fact]
    public async Task AnOrderGoesFromCreationToDelivery_AndTheCustomerIsToldAtEveryStep()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var maria = await ApiUser.SignInAsync("maria", "maria");
        var product = await Scenario.CreateProductAsync(admin, stock: 50, reorderLevel: 5);
        var customerId = await Scenario.EnsureCustomerAsync(admin, "Maria Lopez", MariaEmail);

        var orderId = await Scenario.PlaceOrderAsync(maria, customerId, product, quantity: 2);
        (await Scenario.StatusOfAsync(maria, orderId)).ShouldBe(OrderStatus.Pending);

        // Confirming reserves the stock in Inventory: Orders calls it with its own service identity, not maria's.
        await maria.PostJsonAsync($"/orders/api/v1/orders/{orderId}/confirm");
        (await Scenario.StatusOfAsync(maria, orderId)).ShouldBe(OrderStatus.Confirmed);
        (await Scenario.StockOfAsync(admin, product)).ShouldBe(48);

        // Shipping and delivering are admin-only.
        using (var forbidden = await maria.SendAsync(HttpMethod.Post, $"/orders/api/v1/orders/{orderId}/ship"))
        {
            forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        await admin.PostJsonAsync($"/orders/api/v1/orders/{orderId}/ship");
        await admin.PostJsonAsync($"/orders/api/v1/orders/{orderId}/deliver");
        (await Scenario.StatusOfAsync(maria, orderId)).ShouldBe(OrderStatus.Delivered);

        // One email per step reaches the customer (Orders -> RabbitMQ -> Notifications -> SMTP).
        var emails = await Mailbox.WaitForAsync($"to:{MariaEmail}", expected: 4, email => email.Snippet.Contains(orderId.ToString()));
        emails.Select(email => email.Subject).ShouldBe(
            ["We received your order", "Your order is confirmed", "Your order has shipped", "Your order was delivered"],
            ignoreOrder: true);

        // ...and each one was also recorded by the notifications service.
        var notifications = await admin.GetJsonAsync($"/notifications/api/v1/notifications?orderId={orderId}");
        notifications.GetProperty("totalCount").GetInt32().ShouldBe(4);
    }

    [Fact]
    public async Task CancellingAConfirmedOrder_ReturnsTheStockAndTellsTheCustomer()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var maria = await ApiUser.SignInAsync("maria", "maria");
        var product = await Scenario.CreateProductAsync(admin, stock: 50, reorderLevel: 5);
        var customerId = await Scenario.EnsureCustomerAsync(admin, "Maria Lopez", MariaEmail);

        var orderId = await Scenario.PlaceOrderAsync(maria, customerId, product, quantity: 3);
        await maria.PostJsonAsync($"/orders/api/v1/orders/{orderId}/confirm");
        (await Scenario.StockOfAsync(admin, product)).ShouldBe(47);

        await maria.PostJsonAsync($"/orders/api/v1/orders/{orderId}/cancel");

        (await Scenario.StatusOfAsync(maria, orderId)).ShouldBe(OrderStatus.Cancelled);
        (await Scenario.StockOfAsync(admin, product)).ShouldBe(50);
        var emails = await Mailbox.WaitForAsync($"to:{MariaEmail}", expected: 3, email => email.Snippet.Contains(orderId.ToString()));
        emails.Select(email => email.Subject).ShouldContain("Your order was cancelled");
    }

    [Fact]
    public async Task ConfirmingMoreThanTheStock_IsRefusedAndLeavesTheStockUntouched()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var maria = await ApiUser.SignInAsync("maria", "maria");
        var product = await Scenario.CreateProductAsync(admin, stock: 2, reorderLevel: 0);
        var customerId = await Scenario.EnsureCustomerAsync(admin, "Maria Lopez", MariaEmail);
        var orderId = await Scenario.PlaceOrderAsync(maria, customerId, product, quantity: 5);

        using var response = await maria.SendAsync(HttpMethod.Post, $"/orders/api/v1/orders/{orderId}/confirm");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await Scenario.StatusOfAsync(maria, orderId)).ShouldBe(OrderStatus.Pending);
        (await Scenario.StockOfAsync(admin, product)).ShouldBe(2);
    }
}
