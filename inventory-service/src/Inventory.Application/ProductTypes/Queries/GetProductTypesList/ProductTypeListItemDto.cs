using Inventory.Domain.ProductTypes;

namespace Inventory.Application.ProductTypes.Queries.GetProductTypesList;

public sealed record ProductTypeListItemDto(Guid Id, string Name, string? Description)
{
    public static ProductTypeListItemDto FromDomain(ProductType productType) => new(productType.Id, productType.Name, productType.Description);
}
