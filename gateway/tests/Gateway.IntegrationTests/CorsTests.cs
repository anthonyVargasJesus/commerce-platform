using System.Net;
using Shouldly;

namespace Gateway.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CorsTests(GatewayApiFactory factory)
{
    private const string AllowedOrigin = "http://localhost:5173";

    private static HttpRequestMessage Preflight(string origin, string path = "/orders/api/v1/orders")
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    [Theory]
    [InlineData("/inventory/api/v1/products")]
    [InlineData("/orders/api/v1/orders")]
    [InlineData("/notifications/api/v1/notifications")]
    public async Task Preflight_FromAnAllowedOrigin_IsAnsweredWithoutAToken(string path)
    {
        // Browsers send the preflight without credentials: it must not be rejected with 401.
        var response = await factory.CreateClient().SendAsync(Preflight(AllowedOrigin, path));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([AllowedOrigin]);
        string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")).ToLowerInvariant().ShouldContain("authorization");
    }

    [Fact]
    public async Task Preflight_FromAnUnknownOrigin_GetsNoCorsHeaders()
    {
        var response = await factory.CreateClient().SendAsync(Preflight("https://evil.example.com"));

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task ARealRequestFromAnAllowedOrigin_CarriesTheCorsHeader()
    {
        factory.Downstream
            .Given(WireMock.RequestBuilders.Request.Create().WithPath("/api/v1/cors-check").UsingGet())
            .RespondWith(WireMock.ResponseBuilders.Response.Create().WithStatusCode(200));
        var client = factory.CreateClientWithRoles("customer");
        var request = new HttpRequestMessage(HttpMethod.Get, "/orders/api/v1/cors-check");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe([AllowedOrigin]);
    }
}
