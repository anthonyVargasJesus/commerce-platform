using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Platform.E2ETests;

public static class Endpoints
{
    public static Uri Gateway { get; } = new(Environment.GetEnvironmentVariable("E2E_GATEWAY_URL") ?? "http://localhost:8000");

    public static Uri Keycloak { get; } = new(Environment.GetEnvironmentVariable("E2E_KEYCLOAK_URL") ?? "http://localhost:8180");

    public static Uri Mailpit { get; } = new(Environment.GetEnvironmentVariable("E2E_MAILPIT_URL") ?? "http://localhost:8025");

    public static Uri Dashboard { get; } = new(Environment.GetEnvironmentVariable("E2E_DASHBOARD_URL") ?? "http://localhost:18888");
}

// A signed-in user of the platform: every request goes through the gateway with that user's Keycloak token.
public sealed class ApiUser
{
    private readonly HttpClient _http;

    private ApiUser(HttpClient http) => _http = http;

    public static async Task<ApiUser> SignInAsync(string username, string password)
    {
        using var keycloak = new HttpClient { BaseAddress = Endpoints.Keycloak };
        using var response = await keycloak.PostAsync(
            "/realms/commerce/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "commerce-web",
                ["username"] = username,
                ["password"] = password,
            }));
        response.EnsureSuccessStatusCode();

        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString()!;

        var http = new HttpClient { BaseAddress = Endpoints.Gateway };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new ApiUser(http);
    }

    // A client with no token at all.
    public static HttpClient Anonymous() => new() { BaseAddress = Endpoints.Gateway };

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, IReadOnlyDictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(method, path);
        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
        {
            request.Headers.Add(name, value);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return _http.SendAsync(request);
    }

    public async Task<JsonElement> GetJsonAsync(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<JsonElement> PostJsonAsync(string path, object? body = null)
    {
        using var response = await SendAsync(HttpMethod.Post, path, body);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
