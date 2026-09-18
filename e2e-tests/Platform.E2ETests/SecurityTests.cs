using System.Net;
using System.Net.Sockets;
using Shouldly;

namespace Platform.E2ETests;

[Collection(PlatformCollection.Name)]
public class SecurityTests
{
    [Theory]
    [InlineData("/inventory/api/v1/products")]
    [InlineData("/orders/api/v1/orders")]
    [InlineData("/notifications/api/v1/notifications")]
    public async Task WithoutAToken_EveryRouteIsRejected(string path)
    {
        using var anonymous = ApiUser.Anonymous();

        using var response = await anonymous.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheGatewayHealthEndpointIsPublic()
    {
        using var anonymous = ApiUser.Anonymous();

        using var response = await anonymous.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ACustomerCannotUseTheAdminFunctions()
    {
        var maria = await ApiUser.SignInAsync("maria", "maria");

        using var createCategory = await maria.SendAsync(HttpMethod.Post, "/inventory/api/v1/categories", new { name = "Not allowed" });
        using var listCustomers = await maria.SendAsync(HttpMethod.Get, "/orders/api/v1/customers");
        using var readNotifications = await maria.SendAsync(HttpMethod.Get, "/notifications/api/v1/notifications");

        createCategory.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        listCustomers.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        readNotifications.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ABrowserPreflightFromTheFrontendOrigin_IsAcceptedWithoutAToken()
    {
        using var anonymous = ApiUser.Anonymous();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/orders/api/v1/orders");
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        using var response = await anonymous.SendAsync(preflight);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["http://localhost:5173"]);
    }

    [Fact]
    public async Task TheServicesAreOnlyReachableThroughTheGateway()
    {
        // The direct-access override deliberately publishes these ports; it is not what this test is about.
        if (Environment.GetEnvironmentVariable("E2E_DIRECT_ACCESS") == "1")
        {
            return;
        }

        foreach (var port in new[] { 8080, 8081, 8082 })
        {
            (await CanConnectAsync(port)).ShouldBeFalse($"port {port} should not be published");
        }
    }

    private static async Task<bool> CanConnectAsync(int port)
    {
        using var client = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await client.ConnectAsync("localhost", port, timeout.Token);
            return true;
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            return false;
        }
    }
}
