using Inventory.Application.Common.Interfaces;
using Inventory.Domain.ProductTypes;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class ProductTypeRepository(InventoryDbContext dbContext) : IProductTypeRepository
{
    public Task<ProductType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.ProductTypes.OrderBy(pt => pt.Name).ToListAsync(cancellationToken);

    public void Add(ProductType productType) => dbContext.ProductTypes.Add(productType);

    public void Remove(ProductType productType) => dbContext.ProductTypes.Remove(productType);
}
