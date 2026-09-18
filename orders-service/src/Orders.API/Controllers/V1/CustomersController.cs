using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Customers.Commands.CreateCustomer;
using Orders.Application.Customers.Commands.DeleteCustomer;
using Orders.Application.Customers.Commands.UpdateCustomer;
using Orders.Application.Customers.Queries.GetCustomerById;
using Orders.Application.Customers.Queries.GetCustomersList;

namespace Orders.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
public class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerListItemDto>>> GetList(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomersListQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedCustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedCustomerDto>> Create(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateCustomerCommand(id, request.Name, request.Email, request.Phone), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCustomerCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateCustomerRequest(string Name, string Email, string? Phone);
