using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Orders.Infrastructure.Security;

// Obtains this service's own access token from Keycloak (OAuth2 client credentials) and caches it,
// so calls to other services are made as "orders-service" and not with the end user's token.
public sealed class ServiceTokenProvider(IHttpClientFactory httpClientFactory, ServiceTokenOptions options, TimeProvider timeProvider)
{
    public const string HttpClientName = "keycloak-token";

    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (IsValid())
        {
            return _token!;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsValid())
            {
                return _token!;
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync(
                options.TokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = options.ClientId,
                    ["client_secret"] = options.ClientSecret,
                }),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("The token endpoint returned an empty body.");

            _token = body.AccessToken;
            _expiresAt = timeProvider.GetUtcNow().AddSeconds(body.ExpiresIn) - ExpirySkew;

            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsValid() => _token is not null && timeProvider.GetUtcNow() < _expiresAt;

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
