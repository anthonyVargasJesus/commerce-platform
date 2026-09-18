using Shouldly;

namespace Platform.E2ETests;

[Collection(PlatformCollection.Name)]
public class IdempotencyTests
{
    [Fact]
    public async Task ARepeatedStockAdjustment_WithTheSameKey_IsAppliedOnlyOnce()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var product = await Scenario.CreateProductAsync(admin, stock: 10, reorderLevel: 0);
        var key = new Dictionary<string, string> { ["Idempotency-Key"] = Guid.NewGuid().ToString() };
        var path = $"/inventory/api/v1/products/{product.Id}/adjust-stock";

        using var first = await admin.SendAsync(HttpMethod.Post, path, new { delta = -3 }, key);
        using var repeat = await admin.SendAsync(HttpMethod.Post, path, new { delta = -3 }, key);

        first.EnsureSuccessStatusCode();
        repeat.EnsureSuccessStatusCode();
        (await Scenario.StockOfAsync(admin, product)).ShouldBe(7);
    }
}
