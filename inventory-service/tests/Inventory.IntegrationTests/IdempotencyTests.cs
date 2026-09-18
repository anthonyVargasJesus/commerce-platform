using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace Inventory.IntegrationTests;

[Collection(ApiCollection.Name)]
public class IdempotencyTests(InventoryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClientWithRoles("admin");

    private async Task<Guid> CreateProductAsync(int stock)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/products", new
        {
            sku = $"IDEM-{Guid.NewGuid().ToString("N")[..8]}",
            name = "Idempotency widget",
            price = 5m,
            initialQuantity = stock,
            reorderLevel = 0,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> AdjustAsync(Guid productId, int delta, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/adjust-stock") { Content = JsonContent.Create(new { delta }) };
        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return await _client.SendAsync(request);
    }

    private async Task<int> StockOfAsync(Guid productId) =>
        (await _client.GetFromJsonAsync<JsonElement>($"/api/v1/products/{productId}")).GetProperty("quantityOnHand").GetInt32();

    [Fact]
    public async Task ARepeatedRequestWithTheSameKey_IsAppliedOnlyOnce()
    {
        var productId = await CreateProductAsync(stock: 10);
        var key = Guid.NewGuid().ToString();

        var first = await AdjustAsync(productId, -3, key);
        var repeat = await AdjustAsync(productId, -3, key);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        repeat.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StockOfAsync(productId)).ShouldBe(7, "the second request carried the same key");
    }

    [Fact]
    public async Task RequestsWithDifferentKeys_AreAllApplied()
    {
        var productId = await CreateProductAsync(stock: 10);

        await AdjustAsync(productId, -3, Guid.NewGuid().ToString());
        await AdjustAsync(productId, -3, Guid.NewGuid().ToString());

        (await StockOfAsync(productId)).ShouldBe(4);
    }

    [Fact]
    public async Task RequestsWithoutAKey_KeepWorkingAsBefore()
    {
        var productId = await CreateProductAsync(stock: 10);

        await AdjustAsync(productId, -2, key: null);
        await AdjustAsync(productId, -2, key: null);

        (await StockOfAsync(productId)).ShouldBe(6);
    }

    [Fact]
    public async Task ARejectedRequest_DoesNotBurnItsKey()
    {
        var productId = await CreateProductAsync(stock: 2);
        var key = Guid.NewGuid().ToString();

        var rejected = await AdjustAsync(productId, -5, key);
        rejected.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await AdjustAsync(productId, 10, Guid.NewGuid().ToString());

        var retried = await AdjustAsync(productId, -5, key);

        retried.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StockOfAsync(productId)).ShouldBe(7);
    }

    [Fact]
    public async Task ConcurrentRequestsWithTheSameKey_NeverApplyTheChangeTwice()
    {
        var productId = await CreateProductAsync(stock: 20);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => AdjustAsync(productId, -4, key)));

        // Some of them may lose the race for the unique key (500): callers retry and then get the stored answer.
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBeGreaterThanOrEqualTo(1);
        (await StockOfAsync(productId)).ShouldBe(16);
    }
}
