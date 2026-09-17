using MediatR;

namespace Inventory.Application.ProductTypes.Commands.DeleteProductType;

public sealed record DeleteProductTypeCommand(Guid Id) : IRequest;
