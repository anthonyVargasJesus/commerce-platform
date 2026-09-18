using Orders.Domain.Common;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Customers;

public sealed class Customer : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    private Customer()
    {
    }

    public static Customer Create(string name, string email, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Customer email cannot be empty.");
        }

        return new Customer
        {
            Name = name.Trim(),
            Email = email.Trim(),
            Phone = phone?.Trim(),
        };
    }

    public void UpdateDetails(string name, string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Customer email cannot be empty.");
        }

        Name = name.Trim();
        Email = email.Trim();
        Phone = phone?.Trim();
    }
}
