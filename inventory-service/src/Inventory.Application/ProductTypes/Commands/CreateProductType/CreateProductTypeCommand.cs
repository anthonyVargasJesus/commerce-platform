using MediatR;

namespace Inventory.Application.ProductTypes.Commands.CreateProductType;

public sealed record CreateProductTypeCommand(string Name, string? Description) : IRequest<CreatedProductTypeDto>;
