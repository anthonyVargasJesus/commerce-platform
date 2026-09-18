namespace Orders.Infrastructure.Security;

public sealed record ServiceTokenOptions(string TokenEndpoint, string ClientId, string ClientSecret);
