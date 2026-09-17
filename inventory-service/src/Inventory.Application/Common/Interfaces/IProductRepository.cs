using Inventory.Domain.Products;

namespace Inventory.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken);

    Task<bool> ExistsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<bool> ExistsForProductTypeAsync(Guid productTypeId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Product product);
}
