using Moq;
using Notifications.Application.Common.Interfaces;
using Notifications.Application.Notifications.Commands.SendStockAlert;
using Notifications.Domain.Notifications;
using Shouldly;

namespace Notifications.UnitTests.Application;

public class SendStockAlertCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _repository = new();
    private readonly Mock<INotificationSender> _sender = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Handle_ShouldSendAndPersistAStockAlertWithoutOrderOrCustomer()
    {
        var productId = Guid.NewGuid();
        Notification? sent = null;
        _sender
            .Setup(s => s.SendAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => sent = n)
            .Returns(Task.CompletedTask);

        await new SendStockAlertCommandHandler(_repository.Object, _sender.Object, _unitOfWork.Object)
            .Handle(new SendStockAlertCommand(productId, "SKU-9", "Widget", 2, 5), CancellationToken.None);

        sent.ShouldNotBeNull();
        sent.Type.ShouldBe(NotificationType.LowStock);
        sent.ProductId.ShouldBe(productId);
        sent.OrderId.ShouldBeNull();
        sent.CustomerId.ShouldBeNull();
        sent.Message.ShouldContain("Widget");
        sent.Message.ShouldContain("2 units");
        _repository.Verify(r => r.Add(sent), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
