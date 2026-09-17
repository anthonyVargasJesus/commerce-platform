using Inventory.Domain.Categories;

namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed record CreatedCategoryDto(Guid Id, string Name, string? Description)
{
    public static CreatedCategoryDto FromDomain(Category category) => new(category.Id, category.Name, category.Description);
}
