namespace Orders.Application.Common.Security;

public sealed record OrderScope(bool IsUnrestricted, Guid? CustomerId)
{
    public static OrderScope Unrestricted { get; } = new(true, null);

    // A user with no customer profile: sees no orders at all.
    public static OrderScope Nothing { get; } = new(false, null);

    public static OrderScope OnlyCustomer(Guid customerId) => new(false, customerId);

    public bool HasNoAccess => !IsUnrestricted && CustomerId is null;

    public bool Allows(Guid customerId) => IsUnrestricted || CustomerId == customerId;
}
