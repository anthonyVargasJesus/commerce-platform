using System.Net;
using System.Net.Http.Headers;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Gateway.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthenticationTests(GatewayApiFactory factory)
{
    [Theory]
    [InlineData("/inventory/api/v1/products")]
    [InlineData("/orders/api/v1/orders")]
    [InlineData("/notifications/api/v1/notifications")]
    public async Task WithoutToken_EveryRoute_ShouldReturnUnauthorized(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GarbageToken_ShouldReturnUnauthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/orders/api/v1/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenForAnotherAudience_ShouldReturnUnauthorized()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(["admin"], audience: "another-api"));

        var response = await client.GetAsync("/orders/api/v1/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidToken_ShouldBeForwardedToTheServiceUnchanged()
    {
        factory.Downstream
            .Given(Request.Create().WithPath("/api/v1/forwarding-check").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));
        var client = factory.CreateClientWithRoles("customer");
        var sentToken = client.DefaultRequestHeaders.Authorization!.Parameter;

        var response = await client.GetAsync("/orders/api/v1/forwarding-check");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var received = factory.Downstream.LogEntries.Last(entry => entry.RequestMessage?.Path == "/api/v1/forwarding-check");
        received.RequestMessage!.Headers!["Authorization"].ShouldContain($"Bearer {sentToken}");
    }

    [Fact]
    public async Task HealthLive_ShouldStayAnonymous()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
