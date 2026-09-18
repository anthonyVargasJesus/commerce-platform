using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using MediatR;

namespace Orders.Application.Customers.Commands.DeleteCustomer;

public sealed class DeleteCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteCustomerCommand>
{
    public async Task Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Customers.Customer), request.Id);

        if (await orderRepository.ExistsForCustomerAsync(request.Id, cancellationToken))
        {
            throw new ConflictException($"Customer '{customer.Name}' cannot be deleted because it still has orders.");
        }

        customerRepository.Remove(customer);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
