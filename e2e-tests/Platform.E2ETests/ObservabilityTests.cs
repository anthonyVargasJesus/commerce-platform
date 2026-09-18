using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace Platform.E2ETests;

[Collection(PlatformCollection.Name)]
public class ObservabilityTests
{
    [Fact]
    public async Task OneRequestLeavesASingleTraceAcrossTheGatewayTheServicesAndRabbitMq()
    {
        var admin = await ApiUser.SignInAsync("admin", "admin");
        var maria = await ApiUser.SignInAsync("maria", "maria");
        var product = await Scenario.CreateProductAsync(admin, stock: 50, reorderLevel: 5);
        var customerId = await Scenario.EnsureCustomerAsync(admin, "Maria Lopez", "maria@example.com");
        var orderId = await Scenario.PlaceOrderAsync(maria, customerId, product, quantity: 1);

        // Confirming: gateway -> orders -> inventory (HTTP), and the OrderConfirmed event -> RabbitMQ -> notifications.
        await maria.PostJsonAsync($"/orders/api/v1/orders/{orderId}/confirm");

        var expected = new[] { "Gateway.API", "Orders.API", "Inventory.API", "Notifications.API" };
        using var dashboard = new HttpClient { BaseAddress = Endpoints.Dashboard };
        var deadline = DateTime.UtcNow.AddSeconds(90);
        IReadOnlyCollection<string> best = [];

        while (DateTime.UtcNow < deadline)
        {
            var traces = await dashboard.GetFromJsonAsync<JsonElement>("/api/telemetry/traces");
            var servicesByTrace = new Dictionary<string, HashSet<string>>();

            foreach (var resource in traces.GetProperty("data").GetProperty("resourceSpans").EnumerateArray())
            {
                var service = resource.GetProperty("resource").GetProperty("attributes").EnumerateArray()
                    .First(attribute => attribute.GetProperty("key").GetString() == "service.name")
                    .GetProperty("value").GetProperty("stringValue").GetString()!;

                foreach (var span in resource.GetProperty("scopeSpans").EnumerateArray().SelectMany(scope => scope.GetProperty("spans").EnumerateArray()))
                {
                    var traceId = span.GetProperty("traceId").GetString()!;
                    if (!servicesByTrace.TryGetValue(traceId, out var services))
                    {
                        servicesByTrace[traceId] = services = [];
                    }

                    services.Add(service);
                }
            }

            best = servicesByTrace.Values.OrderByDescending(services => services.Count(expected.Contains)).First();
            if (expected.All(best.Contains))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        best.ShouldBe(expected, ignoreOrder: true, "no single trace crossed all four services");
    }
}
