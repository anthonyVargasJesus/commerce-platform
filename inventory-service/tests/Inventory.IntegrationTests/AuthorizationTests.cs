using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace Inventory.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(InventoryApiFactory factory)
{
    private HttpClient ClientWith(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Anonymous_GetProducts_ShouldReturnUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/products");

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
    [InlineData("service")]
    public async Task AnyAuthenticatedRole_GetProducts_ShouldBeAllowed(string role)
    {
        var response = await factory.CreateClientWithRoles(role).GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("POST", "/api/v1/products")]
    [InlineData("PUT", "/api/v1/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/products/00000000-0000-0000-0000-000000000001/adjust-stock")]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("POST", "/api/v1/product-types")]
    public async Task Customer_WriteOperations_ShouldBeForbidden(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };

        var response = await factory.CreateClientWithRoles("customer").SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Service_AdjustStock_ShouldPassAuthorization()
    {
        var response = await factory.CreateClientWithRoles("service")
            .PostAsJsonAsync("/api/v1/products/00000000-0000-0000-0000-000000000001/adjust-stock", new { delta = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Service_CreateProduct_ShouldBeForbidden()
    {
        var response = await factory.CreateClientWithRoles("service").PostAsJsonAsync("/api/v1/products", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TokenForAnotherAudience_ShouldReturnUnauthorized()
    {
        var response = await ClientWith(TestTokens.Create(["admin"], audience: "another-api")).GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredToken_ShouldReturnUnauthorized()
    {
        var response = await ClientWith(TestTokens.Create(["admin"], lifetime: TimeSpan.FromHours(-1))).GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
