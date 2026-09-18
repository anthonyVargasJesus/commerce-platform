using System.Net;
using System.Net.Http.Json;
using Orders.Application.Customers.Commands.CreateCustomer;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Application.Orders.Dtos;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Orders.IntegrationTests;

[Collection(ApiCollection.Name)]
public class InventoryRetryTests(OrdersApiFactory factory)
{
    [Fact]
    public async Task AdjustStock_WhenInventoryFailsOnce_IsRetriedWithTheSameIdempotencyKey()
    {
        var admin = factory.CreateClientWithRoles("admin");
        var productId = Guid.NewGuid();
        factory.InventoryServer
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}").WithHeader("Authorization", "Bearer service-token").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { id = productId, sku = "SKU-RETRY", name = "Widget", price = 10m, isActive = true }));

        // Inventory fails on the first adjust-stock and answers correctly to the retry.
        var adjustStock = $"/api/v1/products/{productId}/adjust-stock";
        var calls = 0;
        factory.InventoryServer
            .Given(Request.Create().WithPath(adjustStock).UsingPost())
            .RespondWith(Response.Create().WithCallback(_ => new WireMock.ResponseMessage { StatusCode = Interlocked.Increment(ref calls) == 1 ? 500 : 200 }));

        var customer = await (await admin.PostAsJsonAsync("/api/v1/customers", new CreateCustomerCommand("Retry Test", $"{Guid.NewGuid():N}@example.com", null)))
            .Content.ReadFromJsonAsync<CreatedCustomerDto>();
        var order = await (await admin.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(customer!.Id, [new CreateOrderItemRequest(productId, 1)])))
            .Content.ReadFromJsonAsync<OrderDto>();

        var confirm = await admin.PostAsync($"/api/v1/orders/{order!.Id}/confirm", null);

        confirm.StatusCode.ShouldBe(HttpStatusCode.OK);
        var attempts = factory.InventoryServer.LogEntries.Where(entry => entry.RequestMessage?.Path == adjustStock).ToList();
        attempts.Count.ShouldBe(2, "the transient failure is retried");
        var keys = attempts.Select(attempt => attempt.RequestMessage!.Headers!.TryGetValue("Idempotency-Key", out var value) ? value.Single() : null).ToList();
        keys[0].ShouldNotBeNullOrWhiteSpace("Inventory needs a key to recognise a repeated request");
        keys[1].ShouldBe(keys[0], "a retry is the same request and must carry the same key");
    }
}
