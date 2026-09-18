using Moq;
using Notifications.Application.Common.Exceptions;
using Notifications.Application.Common.Interfaces;
using Notifications.Application.Notifications.Queries.GetNotificationById;
using Notifications.Domain.Notifications;
using Shouldly;

namespace Notifications.UnitTests.Application;

public class GetNotificationByIdQueryHandlerTests
{
    private readonly Mock<INotificationRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenNotificationExists_ShouldReturnDto()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.OrderShipped, "Shipped");
        _repository.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var result = await new GetNotificationByIdQueryHandler(_repository.Object)
            .Handle(new GetNotificationByIdQuery(notification.Id), CancellationToken.None);

        result.Id.ShouldBe(notification.Id);
        result.Type.ShouldBe(NotificationType.OrderShipped);
    }

    [Fact]
    public async Task Handle_WhenNotificationDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Notification?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => new GetNotificationByIdQueryHandler(_repository.Object).Handle(new GetNotificationByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
