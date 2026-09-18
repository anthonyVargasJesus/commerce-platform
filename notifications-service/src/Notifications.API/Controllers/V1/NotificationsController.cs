using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Common.Models;
using Notifications.Application.Notifications.Dtos;
using Notifications.Application.Notifications.Queries.GetNotificationById;
using Notifications.Application.Notifications.Queries.GetNotificationsList;

namespace Notifications.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedList<NotificationDto>>> GetList(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? orderId,
        [FromQuery] Guid? productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetNotificationsListQuery(customerId, orderId, productId, pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNotificationByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}
