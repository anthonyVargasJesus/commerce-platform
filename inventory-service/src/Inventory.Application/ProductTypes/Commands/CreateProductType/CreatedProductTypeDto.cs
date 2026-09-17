using Inventory.Domain.ProductTypes;

namespace Inventory.Application.ProductTypes.Commands.CreateProductType;

public sealed record CreatedProductTypeDto(Guid Id, string Name, string? Description)
{
    public static CreatedProductTypeDto FromDomain(ProductType productType) => new(productType.Id, productType.Name, productType.Description);
}
