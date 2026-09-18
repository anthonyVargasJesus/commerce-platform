using System.Net;
using Shouldly;

namespace Platform.E2ETests;

[Collection(PlatformCollection.Name)]
public class OwnershipAndAlertsTests
{
    [Fact]
    public async Task ACustomerOnlySeesAndTouchesTheirOwnOrders()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var maria = await ApiUser.SignInAsync("maria", "maria");
        var product = await Scenario.CreateProductAsync(admin, stock: 50, reorderLevel: 5);
        var mariaCustomer = await Scenario.EnsureCustomerAsync(admin, "Maria Lopez", "maria@example.com");
        var otherCustomer = await Scenario.EnsureCustomerAsync(admin, "Someone Else", $"e2e-{Guid.NewGuid():N}@example.com");
        var othersOrder = await Scenario.PlaceOrderAsync(admin, otherCustomer, product, quantity: 1);
        var myOrder = await Scenario.PlaceOrderAsync(maria, mariaCustomer, product, quantity: 1);

        var visibleToMaria = (await maria.GetJsonAsync("/orders/api/v1/orders?pageSize=100")).GetProperty("items")
            .EnumerateArray().Select(order => order.GetProperty("id").GetGuid()).ToList();
        visibleToMaria.ShouldContain(myOrder);
        visibleToMaria.ShouldNotContain(othersOrder);

        // Someone else's order does not exist as far as maria is concerned (404, not 403).
        foreach (var (method, path) in new[]
        {
            (HttpMethod.Get, $"/orders/api/v1/orders/{othersOrder}"),
            (HttpMethod.Post, $"/orders/api/v1/orders/{othersOrder}/confirm"),
            (HttpMethod.Post, $"/orders/api/v1/orders/{othersOrder}/cancel"),
        })
        {
            using var response = await maria.SendAsync(method, path);
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound, $"{method} {path}");
        }

        using var forSomeoneElse = await maria.SendAsync(
            HttpMethod.Post,
            "/orders/api/v1/orders",
            new { customerId = otherCustomer, items = new[] { new { productId = product.Id, quantity = 1 } } });
        forSomeoneElse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var visibleToAdmin = (await admin.GetJsonAsync("/orders/api/v1/orders?pageSize=100")).GetProperty("items")
            .EnumerateArray().Select(order => order.GetProperty("id").GetGuid()).ToList();
        visibleToAdmin.ShouldContain(othersOrder);
        visibleToAdmin.ShouldContain(myOrder);
    }

    [Fact]
    public async Task WhenStockRunsLow_OperationsIsAlerted()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var product = await Scenario.CreateProductAsync(admin, stock: 10, reorderLevel: 5);

        using var adjustment = await admin.SendAsync(HttpMethod.Post, $"/inventory/api/v1/products/{product.Id}/adjust-stock", new { delta = -6 });
        adjustment.EnsureSuccessStatusCode();

        // Inventory -> RabbitMQ -> Notifications -> the operations mailbox.
        var emails = await Mailbox.WaitForAsync("to:ops@commerce-platform.local", expected: 1, email => email.Snippet.Contains(product.Name));
        emails.ShouldHaveSingleItem().Subject.ShouldBe("Low stock alert");

        var alerts = await admin.GetJsonAsync($"/notifications/api/v1/notifications?productId={product.Id}");
        alerts.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }
}
