using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using MediatR;

namespace Orders.Application.Customers.Commands.DeleteCustomer;

public sealed class DeleteCustomerCommandHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCustomerCommand>
{
    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Customers.Customer), request.Id);

        repository.Remove(customer);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
