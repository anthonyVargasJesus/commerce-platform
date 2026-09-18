using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Orders.IntegrationTests;

// Tokens shaped like the ones Keycloak issues (issuer, audience and roles under realm_access), signed with a
// test key that the factory installs as the only accepted signing key.
public static class TestTokens
{
    public const string Issuer = "http://localhost:8180/realms/commerce";
    public const string Audience = "commerce-platform";

    public static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes("integration-tests-signing-key-0123456789"));

    public static string Create(string[] roles, string audience = Audience, TimeSpan? lifetime = null, string email = "test-user@example.com")
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime ?? TimeSpan.FromHours(1));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            NotBefore = now.AddHours(-2),
            IssuedAt = now.AddHours(-2),
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = Guid.NewGuid().ToString(),
                ["preferred_username"] = "test-user",
                ["email"] = email,
                ["realm_access"] = new Dictionary<string, object> { ["roles"] = roles },
            },
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static AuthenticationHeaderValue Bearer(params string[] roles) => new("Bearer", Create(roles));

    public static AuthenticationHeaderValue BearerFor(string email, params string[] roles) => new("Bearer", Create(roles, email: email));
}
