using Asp.Versioning;
using Inventory.Application.ProductTypes.Commands.CreateProductType;
using Inventory.Application.ProductTypes.Commands.DeleteProductType;
using Inventory.Application.ProductTypes.Commands.UpdateProductType;
using Inventory.Application.ProductTypes.Queries.GetProductTypeById;
using Inventory.Application.ProductTypes.Queries.GetProductTypesList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/product-types")]
public class ProductTypesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductTypeListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductTypeListItemDto>>> GetList(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductTypesListQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductTypeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductTypeDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductTypeByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedProductTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedProductTypeDto>> Create(CreateProductTypeCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateProductTypeRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateProductTypeCommand(id, request.Name, request.Description), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProductTypeCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateProductTypeRequest(string Name, string? Description);
