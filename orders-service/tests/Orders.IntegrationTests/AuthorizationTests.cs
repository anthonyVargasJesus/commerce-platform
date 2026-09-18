using System.Net;
using System.Net.Http.Headers;
using Shouldly;

namespace Orders.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(OrdersApiFactory factory)
{
    private const string UnknownId = "00000000-0000-0000-0000-000000000001";

    [Fact]
    public async Task Anonymous_GetOrders_ShouldReturnUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthLive_ShouldStayAnonymous()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("admin")]
    public async Task CustomerAndAdmin_GetOrders_ShouldBeAllowed(string role)
    {
        var response = await factory.CreateClientWithRoles(role).GetAsync("/api/v1/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("ship")]
    [InlineData("deliver")]
    public async Task Customer_ShipAndDeliver_ShouldBeForbidden(string action)
    {
        var response = await factory.CreateClientWithRoles("customer").PostAsync($"/api/v1/orders/{UnknownId}/{action}", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_Ship_ShouldPassAuthorization()
    {
        var response = await factory.CreateClientWithRoles("admin").PostAsync($"/api/v1/orders/{UnknownId}/ship", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customers_ShouldBeForbiddenForCustomersButAllowedForAdmins()
    {
        (await factory.CreateClientWithRoles("customer").GetAsync("/api/v1/customers")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await factory.CreateClientWithRoles("admin").GetAsync("/api/v1/customers")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TokenForAnotherAudience_ShouldReturnUnauthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(["admin"], audience: "another-api"));

        var response = await client.GetAsync("/api/v1/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
