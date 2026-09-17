using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Products;

public sealed record Sku
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("SKU cannot be empty.");
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 32)
        {
            throw new DomainException("SKU cannot exceed 32 characters.");
        }

        return new Sku(normalized);
    }

    public override string ToString() => Value;
}
