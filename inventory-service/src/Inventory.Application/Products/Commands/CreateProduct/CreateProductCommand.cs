using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int InitialQuantity,
    int ReorderLevel,
    Guid? CategoryId = null,
    Guid? ProductTypeId = null) : IRequest<ProductDto>;
