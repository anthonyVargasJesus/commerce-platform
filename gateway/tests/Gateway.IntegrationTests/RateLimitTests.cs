using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Gateway.IntegrationTests;

[Collection(ApiCollection.Name)]
public class RateLimitTests(GatewayApiFactory factory)
{
    private HttpClient ClientWithLimit(int permitLimit) => factory
        .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:PermitLimit"] = permitLimit.ToString() })))
        .CreateClient();

    private static HttpClient AsNewUser(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = TestTokens.Bearer("customer");
        return client;
    }

    private void StubDownstream() => factory.Downstream
        .Given(Request.Create().WithPath("/api/v1/rate-check").UsingGet())
        .RespondWith(Response.Create().WithStatusCode(200));

    [Fact]
    public async Task AUserOverTheLimit_GetsTooManyRequestsWithRetryAfter()
    {
        StubDownstream();
        var user = AsNewUser(ClientWithLimit(3));

        var statuses = new List<HttpStatusCode>();
        HttpResponseMessage? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await user.GetAsync("/orders/api/v1/rate-check");
            statuses.Add(last.StatusCode);
        }

        statuses.ShouldBe([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests]);
        last!.Headers.GetValues("Retry-After").Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TheLimitIsPerUser_AnotherUserIsUnaffected()
    {
        StubDownstream();
        var noisy = AsNewUser(ClientWithLimit(2));
        for (var i = 0; i < 3; i++)
        {
            await noisy.GetAsync("/orders/api/v1/rate-check");
        }

        var other = AsNewUser(ClientWithLimit(2));

        (await noisy.GetAsync("/orders/api/v1/rate-check")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await other.GetAsync("/orders/api/v1/rate-check")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnauthenticatedFloods_AreLimitedToo()
    {
        var anonymous = ClientWithLimit(2);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            statuses.Add((await anonymous.GetAsync("/orders/api/v1/orders")).StatusCode);
        }

        statuses.ShouldBe([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests]);
    }

    [Fact]
    public async Task TheHealthEndpoint_IsNeverLimited()
    {
        var client = ClientWithLimit(1);

        for (var i = 0; i < 5; i++)
        {
            (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
