using Inventory.Domain.Products;

namespace Inventory.Application.Products.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int QuantityOnHand,
    int ReorderLevel,
    bool IsActive,
    DateTimeOffset CreatedAt,
    Guid? CategoryId,
    Guid? ProductTypeId)
{
    public static ProductDto FromDomain(Product product) => new(
        product.Id,
        product.Sku.Value,
        product.Name,
        product.Description,
        product.Price,
        product.QuantityOnHand,
        product.ReorderLevel,
        product.IsActive,
        product.CreatedAt,
        product.CategoryId,
        product.ProductTypeId);
}
