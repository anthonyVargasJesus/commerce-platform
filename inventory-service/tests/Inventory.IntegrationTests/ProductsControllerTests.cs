using System.Net;
using System.Net.Http.Json;
using Inventory.API.Controllers.V1;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Dtos;
using Shouldly;

namespace Inventory.IntegrationTests;

public class ProductsControllerTests(InventoryApiFactory factory) : IClassFixture<InventoryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateAndGetProduct_ShouldRoundTrip()
    {
        var command = new CreateProductCommand($"SKU-{Guid.NewGuid().ToString("N")[..8]}", "Integration Widget", "desc", 12.5m, 20, 5);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", command);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();
        created.ShouldNotBeNull();

        var getResponse = await _client.GetAsync($"/api/v1/products/{created!.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        fetched!.Name.ShouldBe("Integration Widget");
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdjustStock_BelowZero_ShouldReturnUnprocessableEntity()
    {
        var command = new CreateProductCommand($"SKU-{Guid.NewGuid().ToString("N")[..8]}", "Low Stock Widget", null, 5m, 1, 0);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", command);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var response = await _client.PostAsJsonAsync($"/api/v1/products/{created!.Id}/adjust-stock", new AdjustStockRequest(-10));

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AdjustStock_WithMissingDelta_ShouldReturnBadRequest()
    {
        var command = new CreateProductCommand($"SKU-{Guid.NewGuid().ToString("N")[..8]}", "Widget Missing Delta", null, 5m, 1, 0);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", command);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var response = await _client.PostAsJsonAsync($"/api/v1/products/{created!.Id}/adjust-stock", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
