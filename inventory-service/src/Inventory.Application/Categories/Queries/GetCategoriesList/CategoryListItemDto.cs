using Inventory.Domain.Categories;

namespace Inventory.Application.Categories.Queries.GetCategoriesList;

public sealed record CategoryListItemDto(Guid Id, string Name, string? Description)
{
    public static CategoryListItemDto FromDomain(Category category) => new(category.Id, category.Name, category.Description);
}
