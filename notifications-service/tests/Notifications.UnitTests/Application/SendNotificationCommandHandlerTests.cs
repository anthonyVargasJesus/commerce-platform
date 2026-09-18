using Moq;
using Notifications.Application.Common.Interfaces;
using Notifications.Application.Notifications.Commands.SendNotification;
using Notifications.Domain.Notifications;
using Shouldly;

namespace Notifications.UnitTests.Application;

public class SendNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repository = new();
    private readonly Mock<INotificationSender> _sender = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SendNotificationCommandHandler CreateHandler() => new(_repository.Object, _sender.Object, _unitOfWork.Object);

    [Theory]
    [InlineData(NotificationType.OrderCreated)]
    [InlineData(NotificationType.OrderConfirmed)]
    [InlineData(NotificationType.OrderShipped)]
    [InlineData(NotificationType.OrderDelivered)]
    [InlineData(NotificationType.OrderCancelled)]
    public async Task Handle_ForEveryType_ShouldSendPersistAndMentionTheOrder(NotificationType type)
    {
        var orderId = Guid.NewGuid();
        Notification? sent = null;
        _sender
            .Setup(s => s.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => sent = n)
            .Returns(Task.CompletedTask);

        await CreateHandler().Handle(new SendNotificationCommand(orderId, Guid.NewGuid(), type, 49.5m), CancellationToken.None);

        sent.ShouldNotBeNull();
        sent.Type.ShouldBe(type);
        sent.Message.ShouldContain(orderId.ToString());
        _repository.Verify(r => r.Add(sent), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSenderFails_ShouldNotPersistTheNotification()
    {
        _sender
            .Setup(s => s.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("channel down"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => CreateHandler().Handle(new SendNotificationCommand(Guid.NewGuid(), Guid.NewGuid(), NotificationType.OrderConfirmed, 10m), CancellationToken.None));

        _repository.Verify(r => r.Add(It.IsAny<Notification>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
