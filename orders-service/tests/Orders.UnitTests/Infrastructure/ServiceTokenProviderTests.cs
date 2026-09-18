using System.Net;
using System.Text;
using Orders.Infrastructure.Security;
using Shouldly;

namespace Orders.UnitTests.Infrastructure;

public class ServiceTokenProviderTests
{
    private static readonly ServiceTokenOptions Options = new("http://keycloak/token", "orders-service", "secret");

    private sealed class RecordingHandler(Func<int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return respond(RequestBodies.Count);
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static HttpResponseMessage Token(string value, int expiresIn) => new(HttpStatusCode.OK)
    {
        Content = new StringContent($"{{\"access_token\":\"{value}\",\"expires_in\":{expiresIn}}}", Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task GetToken_ShouldRequestAClientCredentialsToken()
    {
        var handler = new RecordingHandler(_ => Token("token-1", 300));
        var provider = new ServiceTokenProvider(new SingleClientFactory(handler), Options, new FakeClock());

        var token = await provider.GetTokenAsync(CancellationToken.None);

        token.ShouldBe("token-1");
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("grant_type=client_credentials");
        body.ShouldContain("client_id=orders-service");
        body.ShouldContain("client_secret=secret");
    }

    [Fact]
    public async Task GetToken_WhileTheTokenIsValid_ShouldReuseTheCachedOne()
    {
        var handler = new RecordingHandler(_ => Token("token-1", 300));
        var provider = new ServiceTokenProvider(new SingleClientFactory(handler), Options, new FakeClock());

        await provider.GetTokenAsync(CancellationToken.None);
        await provider.GetTokenAsync(CancellationToken.None);
        await provider.GetTokenAsync(CancellationToken.None);

        handler.RequestBodies.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetToken_AfterTheTokenExpires_ShouldRequestANewOne()
    {
        var clock = new FakeClock();
        var handler = new RecordingHandler(call => Token($"token-{call}", 300));
        var provider = new ServiceTokenProvider(new SingleClientFactory(handler), Options, clock);

        var first = await provider.GetTokenAsync(CancellationToken.None);
        clock.Now = clock.Now.AddSeconds(271); // 300 s lifetime minus the 30 s safety margin
        var second = await provider.GetTokenAsync(CancellationToken.None);

        first.ShouldBe("token-1");
        second.ShouldBe("token-2");
    }

    [Fact]
    public async Task GetToken_WhenKeycloakRejectsTheClient_ShouldThrow()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var provider = new ServiceTokenProvider(new SingleClientFactory(handler), Options, new FakeClock());

        await Should.ThrowAsync<HttpRequestException>(() => provider.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Handler_ShouldSendTheServiceTokenAsABearerHeader()
    {
        var tokenHandler = new RecordingHandler(_ => Token("token-1", 300));
        var provider = new ServiceTokenProvider(new SingleClientFactory(tokenHandler), Options, new FakeClock());
        string? authorization = null;
        var inner = new CapturingHandler(request => authorization = request.Headers.Authorization?.ToString());
        var client = new HttpClient(new ServiceTokenHandler(provider) { InnerHandler = inner });

        await client.GetAsync("http://inventory/api/v1/products");

        authorization.ShouldBe("Bearer token-1");
    }

    private sealed class CapturingHandler(Action<HttpRequestMessage> capture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
