using Inventory.Domain.Categories;

namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed record CategoryDetailsDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static CategoryDetailsDto FromDomain(Category category) => new(category.Id, category.Name, category.Description, category.CreatedAt);
}
