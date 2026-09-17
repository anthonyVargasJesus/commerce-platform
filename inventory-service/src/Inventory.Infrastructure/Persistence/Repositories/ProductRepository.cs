using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class ProductRepository(InventoryDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken)
    {
        var normalized = Sku.Create(sku).Value;
        return dbContext.Products.AnyAsync(p => p.Sku.Value == normalized, cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsForCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);

    public Task<bool> ExistsForProductTypeAsync(Guid productTypeId, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(p => p.ProductTypeId == productTypeId, cancellationToken);

    public void Add(Product product) => dbContext.Products.Add(product);
}
