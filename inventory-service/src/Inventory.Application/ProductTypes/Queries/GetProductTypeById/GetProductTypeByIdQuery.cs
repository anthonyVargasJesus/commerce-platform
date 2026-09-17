using MediatR;

namespace Inventory.Application.ProductTypes.Queries.GetProductTypeById;

public sealed record GetProductTypeByIdQuery(Guid Id) : IRequest<ProductTypeDetailsDto>;
