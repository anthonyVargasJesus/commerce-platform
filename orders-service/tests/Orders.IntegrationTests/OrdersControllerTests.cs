using System.Net;
using System.Net.Http.Json;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Application.Orders.Dtos;
using Orders.Domain.Orders;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Orders.IntegrationTests;

public class OrdersControllerTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private void StubProduct(Guid productId, string sku, decimal price, bool isActive = true)
    {
        factory.InventoryServer
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = productId, sku, name = "Widget", price, isActive }));
    }

    private void StubAdjustStock(Guid productId, HttpStatusCode statusCode)
    {
        factory.InventoryServer
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}/adjust-stock").UsingPost())
            .RespondWith(Response.Create().WithStatusCode((int)statusCode));
    }

    [Fact]
    public async Task CreateOrder_WithActiveProduct_ShouldReturnPendingOrder()
    {
        var productId = Guid.NewGuid();
        StubProduct(productId, "SKU-1", 12.5m);

        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(productId, 3)]);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.ShouldBe(OrderStatus.Pending);
        order.TotalAmount.ShouldBe(37.5m);
    }

    [Fact]
    public async Task CreateOrder_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        var productId = Guid.NewGuid();
        factory.InventoryServer
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(productId, 1)]);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", command);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfirmOrder_WithAvailableStock_ShouldTransitionToConfirmed()
    {
        var productId = Guid.NewGuid();
        StubProduct(productId, "SKU-2", 10m);
        StubAdjustStock(productId, HttpStatusCode.OK);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(productId, 2)]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var confirmResponse = await _client.PostAsync($"/api/v1/orders/{created!.Id}/confirm", null);

        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<OrderDto>();
        confirmed!.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task ConfirmOrder_WithInsufficientStock_ShouldReturnConflict()
    {
        var productId = Guid.NewGuid();
        StubProduct(productId, "SKU-3", 10m);
        StubAdjustStock(productId, HttpStatusCode.UnprocessableEntity);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(productId, 100)]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        var confirmResponse = await _client.PostAsync($"/api/v1/orders/{created!.Id}/confirm", null);

        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var getResponse = await _client.GetAsync($"/api/v1/orders/{created.Id}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public async Task CancelOrder_WhenConfirmed_ShouldRestockAndReturnCancelled()
    {
        var productId = Guid.NewGuid();
        StubProduct(productId, "SKU-4", 10m);
        StubAdjustStock(productId, HttpStatusCode.OK);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/orders",
            new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(productId, 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        await _client.PostAsync($"/api/v1/orders/{created!.Id}/confirm", null);

        var cancelResponse = await _client.PostAsync($"/api/v1/orders/{created.Id}/cancel", null);

        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<OrderDto>();
        cancelled!.Status.ShouldBe(OrderStatus.Cancelled);

        // Two adjust-stock calls happened for this specific product: -1 at confirm, +1 at cancel.
        // (Filter by productId, not just the path pattern — the WireMock server instance is
        // shared across every test in this class, so other tests' adjust-stock calls also
        // show up in the log.)
        factory.InventoryServer.LogEntries.Count(e => e.RequestMessage?.Path == $"/api/v1/products/{productId}/adjust-stock").ShouldBe(2);
    }

    [Fact]
    public async Task GetById_WhenOrderDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
