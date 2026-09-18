using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Application.Common.Interfaces;

namespace Orders.Infrastructure.ExternalServices.Inventory;

public class InventoryServiceClient(HttpClient httpClient) : IInventoryServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"api/v1/products/{productId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<InventoryProductResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Inventory returned an empty product body.");

        return new ProductSnapshot(product.Id, product.Sku, product.Name, product.Price, product.IsActive);
    }

    public async Task<InventoryAdjustmentResult> AdjustStockAsync(Guid productId, int delta, string idempotencyKey, CancellationToken cancellationToken)
    {
        // The resilience pipeline may resend this POST after a transient failure. The key is part of the request,
        // so every resend carries the same one and Inventory applies the change only once.
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/products/{productId}/adjust-stock")
        {
            Content = JsonContent.Create(new InventoryAdjustStockRequest(delta), options: JsonOptions),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await httpClient.SendAsync(request, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.OK => InventoryAdjustmentResult.Success,
            HttpStatusCode.NotFound => InventoryAdjustmentResult.ProductNotFound,
            HttpStatusCode.UnprocessableEntity => InventoryAdjustmentResult.InsufficientStock,
            _ => throw new HttpRequestException($"Unexpected response from inventory-service: {(int)response.StatusCode}."),
        };
    }

    private sealed record InventoryProductResponse(Guid Id, string Sku, string Name, decimal Price, bool IsActive);

    private sealed record InventoryAdjustStockRequest(int Delta);
}
