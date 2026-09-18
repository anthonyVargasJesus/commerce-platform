namespace Orders.Application.Common.Interfaces;

public interface ICurrentUser
{
    // Admins and the service account can see every order; any other user is limited to their own.
    bool CanAccessAllOrders { get; }

    // The email in the access token; it links the user to a Customer.
    string? Email { get; }
}
