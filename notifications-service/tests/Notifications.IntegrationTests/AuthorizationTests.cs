using System.Net;
using Shouldly;

namespace Notifications.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(NotificationsApiFactory factory)
{
    [Fact]
    public async Task Anonymous_ShouldReturnUnauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/notifications");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("service")]
    public async Task NonAdminRoles_ShouldBeForbidden(string role)
    {
        var response = await factory.CreateClientWithRoles(role).GetAsync("/api/v1/notifications");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ShouldBeAllowed()
    {
        var response = await factory.CreateClientWithRoles("admin").GetAsync("/api/v1/notifications");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthLive_ShouldStayAnonymous()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
