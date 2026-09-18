using System.Net;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Gateway.IntegrationTests;

public class RoutingTests(GatewayApiFactory factory) : IClassFixture<GatewayApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/inventory/api/v1/products", "/api/v1/products")]
    [InlineData("/orders/api/v1/orders/123", "/api/v1/orders/123")]
    [InlineData("/notifications/api/v1/notifications?orderId=abc", "/api/v1/notifications")]
    public async Task Request_ShouldBeForwardedWithTheServicePrefixRemoved(string gatewayPath, string downstreamPath)
    {
        factory.Downstream
            .Given(Request.Create().WithPath(downstreamPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("ok"));

        var response = await _client.GetAsync(gatewayPath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("ok");
    }

    [Fact]
    public async Task Request_ShouldForwardTheMethodBodyAndDownstreamStatus()
    {
        factory.Downstream
            .Given(Request.Create().WithPath("/api/v1/orders").UsingPost().WithBody("{\"a\":1}"))
            .RespondWith(Response.Create().WithStatusCode(201));

        var response = await _client.PostAsync("/orders/api/v1/orders", new StringContent("{\"a\":1}", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UnknownPrefix_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/billing/api/v1/invoices");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthLive_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
