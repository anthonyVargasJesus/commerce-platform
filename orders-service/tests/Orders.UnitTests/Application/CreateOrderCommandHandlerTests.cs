using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Domain.Customers;
using Orders.Domain.Orders;
using Moq;
using Shouldly;

namespace Orders.UnitTests.Application;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IInventoryServiceClient> _inventoryClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateOrderCommandHandler CreateHandler() =>
        new(_repository.Object, _customerRepository.Object, _inventoryClient.Object, _unitOfWork.Object);

    private Customer SetupExistingCustomer()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        return customer;
    }

    [Fact]
    public async Task Handle_WithActiveProducts_ShouldCreateOrderAndReturnDto()
    {
        var customer = SetupExistingCustomer();
        var productId = Guid.NewGuid();
        _inventoryClient
            .Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSnapshot(productId, "SKU-1", "Widget", 10m, true));

        var command = new CreateOrderCommand(customer.Id, [new CreateOrderItemRequest(productId, 2)]);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TotalAmount.ShouldBe(20m);
        result.Status.ShouldBe(OrderStatus.Pending);
        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ShouldThrowNotFoundException()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(command, CancellationToken.None));

        _inventoryClient.Verify(c => c.GetProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ShouldThrowNotFoundException()
    {
        var customer = SetupExistingCustomer();
        var productId = Guid.NewGuid();
        _inventoryClient
            .Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductSnapshot?)null);

        var command = new CreateOrderCommand(customer.Id, [new CreateOrderItemRequest(productId, 1)]);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(command, CancellationToken.None));

        _repository.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductIsInactive_ShouldThrowConflictException()
    {
        var customer = SetupExistingCustomer();
        var productId = Guid.NewGuid();
        _inventoryClient
            .Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductSnapshot(productId, "SKU-1", "Widget", 10m, false));

        var command = new CreateOrderCommand(customer.Id, [new CreateOrderItemRequest(productId, 1)]);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(command, CancellationToken.None));
    }
}
