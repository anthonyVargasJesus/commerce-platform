using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Inventory.API.Configuration;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddPlatformAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Configuration 'Authentication:Authority' was not found.");
        var audience = configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Configuration 'Authentication:Audience' was not found.");

        // Keycloak stamps every token with the public issuer (http://localhost:8180/...), but inside Docker the
        // metadata and signing keys are fetched from its internal address, so the two are configured separately.
        var metadataAddress = configuration["Authentication:MetadataAddress"] ?? $"{authority}/.well-known/openid-configuration";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidIssuer = authority;
                options.TokenValidationParameters.ValidAudience = audience;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        AddRealmRolesAsRoleClaims(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    // Keycloak puts roles in the realm_access claim ({"roles":["admin"]}); [Authorize(Roles = ...)] needs role claims.
    private static void AddRealmRolesAsRoleClaims(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (realmAccess is null)
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles))
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
        }
    }
}
