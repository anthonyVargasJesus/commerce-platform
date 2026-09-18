using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Customers.Commands.DeleteCustomer;
using Orders.Domain.Customers;
using Moq;
using Shouldly;

namespace Orders.UnitTests.Application;

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private DeleteCustomerCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WhenCustomerExists_ShouldRemoveAndPersist()
    {
        var customer = Customer.Create("Jane Doe", "jane@example.com");
        _repository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        await CreateHandler().Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

        _repository.Verify(r => r.Remove(customer), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCustomerDoesNotExist_ShouldThrowNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateHandler().Handle(new DeleteCustomerCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
