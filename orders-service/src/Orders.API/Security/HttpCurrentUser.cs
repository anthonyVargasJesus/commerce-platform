using Orders.Application.Common.Interfaces;

namespace Orders.API.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool CanAccessAllOrders => User is not null && (User.IsInRole("admin") || User.IsInRole("service"));

    public string? Email => User?.FindFirst("email")?.Value;
}
