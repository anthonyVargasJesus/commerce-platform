using Inventory.Domain.ProductTypes;

namespace Inventory.Application.ProductTypes.Queries.GetProductTypeById;

public sealed record ProductTypeDetailsDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static ProductTypeDetailsDto FromDomain(ProductType productType) => new(productType.Id, productType.Name, productType.Description, productType.CreatedAt);
}
