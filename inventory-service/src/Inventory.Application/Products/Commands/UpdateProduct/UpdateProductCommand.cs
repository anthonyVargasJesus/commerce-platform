using MediatR;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int ReorderLevel,
    Guid? CategoryId = null,
    Guid? ProductTypeId = null) : IRequest;
