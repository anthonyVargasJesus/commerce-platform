using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Customers.Commands.UpdateCustomer;
using Orders.Domain.Customers;
using Moq;
using Shouldly;

namespace Orders.UnitTests.Application;

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateCustomerCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenCustomerExists_ShouldUpdateAndPersist()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");
        _repository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        await CreateHandler().Handle(new UpdateCustomerCommand(customer.Id, "Jane Smith", "jane@example.com", "555-5678"), CancellationToken.None);

        customer.Name.ShouldBe("Jane Smith");
        customer.Phone.ShouldBe("555-5678");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => CreateHandler().Handle(new UpdateCustomerCommand(Guid.NewGuid(), "Name", "email@example.com", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenEmailBelongsToAnotherCustomer_ShouldThrowConflictException()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");
        _repository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repository.Setup(r => r.EmailExistsAsync("taken@example.com", customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Should.ThrowAsync<ConflictException>(
            () => CreateHandler().Handle(new UpdateCustomerCommand(customer.Id, "Jane Doe", "taken@example.com", null), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailIsUnchanged_ShouldNotQueryEmailExistence()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");
        _repository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        await CreateHandler().Handle(new UpdateCustomerCommand(customer.Id, "Jane Doe", "jane@example.com", "555-0000"), CancellationToken.None);

        _repository.Verify(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
