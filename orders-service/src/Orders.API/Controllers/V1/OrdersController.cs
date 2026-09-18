using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Common.Models;
using Orders.Application.Orders.Commands.CancelOrder;
using Orders.Application.Orders.Commands.ConfirmOrder;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Application.Orders.Commands.DeliverOrder;
using Orders.Application.Orders.Commands.ShipOrder;
using Orders.Application.Orders.Dtos;
using Orders.Application.Orders.Queries.GetOrderById;
using Orders.Application.Orders.Queries.GetOrdersList;

namespace Orders.API.Controllers.V1;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<OrderListItemDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetOrdersListQuery(pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Ship(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ShipOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeliverOrderCommand(id), cancellationToken);
        return Ok(result);
    }
}
