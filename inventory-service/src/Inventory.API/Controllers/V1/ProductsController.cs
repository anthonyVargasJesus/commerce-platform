using Asp.Versioning;
using Inventory.Application.Common.Models;
using Inventory.Application.Products.Commands.AdjustStock;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Dtos;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProductsList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ProductDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductsListQuery(pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.ReorderLevel, request.CategoryId, request.ProductTypeId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/adjust-stock")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductDto>> AdjustStock(Guid id, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AdjustStockCommand(id, request.Delta), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateProductRequest(string Name, string? Description, decimal Price, int ReorderLevel, Guid? CategoryId = null, Guid? ProductTypeId = null);

public sealed record AdjustStockRequest(int Delta);
