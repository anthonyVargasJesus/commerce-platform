using Inventory.Domain.ProductTypes;

namespace Inventory.Application.Common.Interfaces;

public interface IProductTypeRepository
{
    Task<ProductType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductType>> GetAllAsync(CancellationToken cancellationToken);

    void Add(ProductType productType);

    void Remove(ProductType productType);
}
