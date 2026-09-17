using MediatR;

namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Description) : IRequest;
