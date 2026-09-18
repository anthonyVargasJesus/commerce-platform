using System.Net.Http.Json;
using System.Text.Json;

namespace Platform.E2ETests;

// Order statuses as the API serializes them (numeric enum).
public static class OrderStatus
{
    public const int Pending = 0;
    public const int Confirmed = 1;
    public const int Shipped = 2;
    public const int Delivered = 3;
    public const int Cancelled = 4;
}

public sealed record Product(Guid Id, string Sku, string Name);

// Data setup shared by the tests. Every test creates its own product, so stock assertions are exact and
// do not depend on the seed data or on what earlier tests did.
public static class Scenario
{
    public static async Task<Product> CreateProductAsync(ApiUser admin, int stock, int reorderLevel)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var product = await admin.PostJsonAsync("/inventory/api/v1/products", new
        {
            sku = $"E2E-{suffix}",
            name = $"E2E Widget {suffix}",
            description = "Created by the end-to-end tests",
            price = 10m,
            initialQuantity = stock,
            reorderLevel,
        });

        return new Product(product.GetProperty("id").GetGuid(), product.GetProperty("sku").GetString()!, product.GetProperty("name").GetString()!);
    }

    public static async Task<int> StockOfAsync(ApiUser admin, Product product) =>
        (await admin.GetJsonAsync($"/inventory/api/v1/products/{product.Id}")).GetProperty("quantityOnHand").GetInt32();

    // The customer profile is what links a Keycloak user to their orders (by email); reuse it when it exists.
    public static async Task<Guid> EnsureCustomerAsync(ApiUser admin, string name, string email)
    {
        var customers = await admin.GetJsonAsync("/orders/api/v1/customers");
        foreach (var customer in customers.EnumerateArray())
        {
            if (string.Equals(customer.GetProperty("email").GetString(), email, StringComparison.OrdinalIgnoreCase))
            {
                return customer.GetProperty("id").GetGuid();
            }
        }

        return (await admin.PostJsonAsync("/orders/api/v1/customers", new { name, email })).GetProperty("id").GetGuid();
    }

    public static async Task<Guid> PlaceOrderAsync(ApiUser user, Guid customerId, Product product, int quantity) =>
        (await user.PostJsonAsync("/orders/api/v1/orders", new { customerId, items = new[] { new { productId = product.Id, quantity } } }))
            .GetProperty("id").GetGuid();

    public static async Task<int> StatusOfAsync(ApiUser user, Guid orderId) =>
        (await user.GetJsonAsync($"/orders/api/v1/orders/{orderId}")).GetProperty("status").GetInt32();
}

public sealed record Email(string Subject, string Snippet);

// Mailpit is the SMTP server of the platform: it captures every email and exposes them over REST.
public static class Mailbox
{
    public static async Task<IReadOnlyList<Email>> WaitForAsync(string query, int expected, Func<Email, bool>? filter = null, TimeSpan? timeout = null)
    {
        using var mailpit = new HttpClient { BaseAddress = Endpoints.Mailpit };
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(45));
        var found = new List<Email>();

        while (DateTime.UtcNow < deadline)
        {
            var result = await mailpit.GetFromJsonAsync<JsonElement>($"/api/v1/search?query={Uri.EscapeDataString(query)}");
            found = result.GetProperty("messages").EnumerateArray()
                .Select(message => new Email(message.GetProperty("Subject").GetString()!, message.GetProperty("Snippet").GetString()!))
                .Where(email => filter?.Invoke(email) ?? true)
                .ToList();

            if (found.Count >= expected)
            {
                return found;
            }

            await Task.Delay(500);
        }

        return found;
    }
}
