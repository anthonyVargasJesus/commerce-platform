using MediatR;

namespace Inventory.Application.ProductTypes.Commands.UpdateProductType;

public sealed record UpdateProductTypeCommand(Guid Id, string Name, string? Description) : IRequest;
