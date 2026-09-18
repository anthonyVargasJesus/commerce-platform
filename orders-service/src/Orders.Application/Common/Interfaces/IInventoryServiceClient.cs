namespace Orders.Application.Common.Interfaces;

/// <summary>
/// Port for the HTTP integration with inventory-service. Infrastructure implements this
/// with a resilient (retry/circuit-breaker/timeout) typed HttpClient; Application only
/// deals with this domain-shaped contract, never with HttpClient/status codes directly.
/// </summary>
public interface IInventoryServiceClient
{
    Task<ProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken);

    Task<InventoryAdjustmentResult> AdjustStockAsync(Guid productId, int delta, CancellationToken cancellationToken);
}

public sealed record ProductSnapshot(Guid Id, string Sku, string Name, decimal Price, bool IsActive);

public enum InventoryAdjustmentResult
{
    Success,
    ProductNotFound,
    InsufficientStock,
}
