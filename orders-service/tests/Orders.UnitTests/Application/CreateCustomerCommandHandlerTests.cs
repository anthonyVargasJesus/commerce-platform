using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Application.Customers.Commands.CreateCustomer;
using Orders.Domain.Customers;
using Moq;
using Shouldly;

namespace Orders.UnitTests.Application;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateCustomerCommandHandler CreateHandler() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithValidData_ShouldAddCustomerAndReturnDto()
    {
        _repository.Setup(r => r.EmailExistsAsync("jane@example.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new CreateCustomerCommand("Jane Doe", "jane@example.com", "555-1234");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.ShouldBe("Jane Doe");
        result.Email.ShouldBe("jane@example.com");
        _repository.Verify(r => r.Add(It.IsAny<Customer>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldThrowConflictException()
    {
        _repository.Setup(r => r.EmailExistsAsync("jane@example.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new CreateCustomerCommand("Jane Doe", "jane@example.com", null);

        await Should.ThrowAsync<ConflictException>(() => CreateHandler().Handle(command, CancellationToken.None));

        _repository.Verify(r => r.Add(It.IsAny<Customer>()), Times.Never);
    }
}
