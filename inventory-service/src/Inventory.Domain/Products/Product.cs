using Inventory.Domain.Common;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Products.Events;

namespace Inventory.Domain.Products;

public sealed class Product : BaseAuditableEntity
{
    public Sku Sku { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public int QuantityOnHand { get; private set; }

    public int ReorderLevel { get; private set; }

    public bool IsActive { get; private set; } = true;

    public Guid? CategoryId { get; private set; }

    public Guid? ProductTypeId { get; private set; }

    private Product()
    {
    }

    public static Product Create(string sku, string name, decimal price, int initialQuantity, int reorderLevel, string? description = null, Guid? categoryId = null, Guid? productTypeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name cannot be empty.");
        }

        if (price < 0)
        {
            throw new DomainException("Product price cannot be negative.");
        }

        if (initialQuantity < 0)
        {
            throw new DomainException("Initial quantity cannot be negative.");
        }

        if (reorderLevel < 0)
        {
            throw new DomainException("Reorder level cannot be negative.");
        }

        var product = new Product
        {
            Sku = Sku.Create(sku),
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = price,
            QuantityOnHand = initialQuantity,
            ReorderLevel = reorderLevel,
            CategoryId = categoryId,
            ProductTypeId = productTypeId,
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Sku.Value));

        return product;
    }

    public void UpdateDetails(string name, string? description, decimal price, int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name cannot be empty.");
        }

        if (price < 0)
        {
            throw new DomainException("Product price cannot be negative.");
        }

        if (reorderLevel < 0)
        {
            throw new DomainException("Reorder level cannot be negative.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        ReorderLevel = reorderLevel;
    }

    public void AdjustStock(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        var newQuantity = QuantityOnHand + delta;

        if (newQuantity < 0)
        {
            throw new InsufficientStockException(Id, Math.Abs(delta), QuantityOnHand);
        }

        QuantityOnHand = newQuantity;

        AddDomainEvent(new StockAdjustedEvent(Id, delta, QuantityOnHand));

        if (QuantityOnHand <= ReorderLevel)
        {
            AddDomainEvent(new ProductLowStockEvent(Id, QuantityOnHand, ReorderLevel));
        }
    }

    public void AssignCategory(Guid? categoryId) => CategoryId = categoryId;

    public void AssignProductType(Guid? productTypeId) => ProductTypeId = productTypeId;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
