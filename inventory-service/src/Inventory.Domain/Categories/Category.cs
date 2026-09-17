using Inventory.Domain.Common;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Categories;

public sealed class Category : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    private Category()
    {
    }

    public static Category Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name cannot be empty.");
        }

        return new Category
        {
            Name = name.Trim(),
            Description = description?.Trim(),
        };
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name cannot be empty.");
        }

        Name = name.Trim();
        Description = description?.Trim();
    }
}
