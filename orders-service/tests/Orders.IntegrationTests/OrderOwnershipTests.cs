using System.Net;
using System.Net.Http.Json;
using Orders.Application.Customers.Commands.CreateCustomer;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Application.Orders.Dtos;
using Orders.Application.Orders.Queries.GetOrdersList;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Orders.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OrderOwnershipTests(OrdersApiFactory factory)
{
    private sealed record OrdersPage(IReadOnlyList<OrderListItemDto> Items);

    private readonly HttpClient _admin = factory.CreateClientWithRoles("admin");

    private Guid StubProduct()
    {
        var productId = Guid.NewGuid();
        factory.InventoryServer
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}").WithHeader("Authorization", "Bearer service-token").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { id = productId, sku = "SKU-OWN", name = "Widget", price = 10m, isActive = true }));
        factory.InventoryServer
            .Given(Request.Create().WithPath($"/api/v1/products/{productId}/adjust-stock").WithHeader("Authorization", "Bearer service-token").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
        return productId;
    }

    private async Task<(string Email, Guid Id)> CreateCustomerAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var response = await _admin.PostAsJsonAsync("/api/v1/customers", new CreateCustomerCommand("Owner Test", email, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CreatedCustomerDto>();
        return (email, customer!.Id);
    }

    private async Task<Guid> CreateOrderAsync(HttpClient client, Guid customerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(customerId, [new CreateOrderItemRequest(StubProduct(), 1)]));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!.Id;
    }

    private static async Task<IReadOnlyList<OrderListItemDto>> ListAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<OrdersPage>("/api/v1/orders?pageSize=100"))!.Items;

    [Fact]
    public async Task ACustomer_ShouldOnlySeeTheirOwnOrders()
    {
        var (emailA, customerA) = await CreateCustomerAsync();
        var (_, customerB) = await CreateCustomerAsync();
        var orderA = await CreateOrderAsync(_admin, customerA);
        var orderB = await CreateOrderAsync(_admin, customerB);
        var a = factory.CreateClientFor(emailA, "customer");

        var visible = await ListAsync(a);

        visible.Select(o => o.Id).ShouldBe([orderA]);
        (await a.GetAsync($"/api/v1/orders/{orderA}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await a.GetAsync($"/api/v1/orders/{orderB}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnAdmin_ShouldSeeEveryonesOrders()
    {
        var (_, customerA) = await CreateCustomerAsync();
        var (_, customerB) = await CreateCustomerAsync();
        var orderA = await CreateOrderAsync(_admin, customerA);
        var orderB = await CreateOrderAsync(_admin, customerB);

        var ids = (await ListAsync(_admin)).Select(o => o.Id).ToList();

        ids.ShouldContain(orderA);
        ids.ShouldContain(orderB);
    }

    [Fact]
    public async Task ACustomer_CannotConfirmOrCancelSomeoneElsesOrder()
    {
        var (emailA, _) = await CreateCustomerAsync();
        var (_, customerB) = await CreateCustomerAsync();
        var orderB = await CreateOrderAsync(_admin, customerB);
        var a = factory.CreateClientFor(emailA, "customer");

        (await a.PostAsync($"/api/v1/orders/{orderB}/confirm", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await a.PostAsync($"/api/v1/orders/{orderB}/cancel", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var untouched = await _admin.GetFromJsonAsync<OrderDto>($"/api/v1/orders/{orderB}");
        untouched!.Status.ToString().ShouldBe("Pending");
    }

    [Fact]
    public async Task ACustomer_CanCreateAndConfirmTheirOwnOrder_ButNotOneForSomeoneElse()
    {
        var (emailA, customerA) = await CreateCustomerAsync();
        var (_, customerB) = await CreateCustomerAsync();
        var a = factory.CreateClientFor(emailA, "customer");

        var forSomeoneElse = await a.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(customerB, [new CreateOrderItemRequest(StubProduct(), 1)]));
        forSomeoneElse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var own = await CreateOrderAsync(a, customerA);
        (await a.PostAsync($"/api/v1/orders/{own}/confirm", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AUserWithoutACustomerProfile_SeesNothingAndCannotOrder()
    {
        var (_, someCustomer) = await CreateCustomerAsync();
        await CreateOrderAsync(_admin, someCustomer);
        var stranger = factory.CreateClientFor($"{Guid.NewGuid():N}@example.com", "customer");

        (await ListAsync(stranger)).ShouldBeEmpty();
        var create = await stranger.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(someCustomer, [new CreateOrderItemRequest(StubProduct(), 1)]));
        create.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
