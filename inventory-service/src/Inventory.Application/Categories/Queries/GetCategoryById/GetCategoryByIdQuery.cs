using MediatR;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDetailsDto>;
