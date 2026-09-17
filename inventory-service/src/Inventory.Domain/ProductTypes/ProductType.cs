using Inventory.Domain.Common;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.ProductTypes;

public sealed class ProductType : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    private ProductType()
    {
    }

    public static ProductType Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product type name cannot be empty.");
        }

        return new ProductType
        {
            Name = name.Trim(),
            Description = description?.Trim(),
        };
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product type name cannot be empty.");
        }

        Name = name.Trim();
        Description = description?.Trim();
    }
}
