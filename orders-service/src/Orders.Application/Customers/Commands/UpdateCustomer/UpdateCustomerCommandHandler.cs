using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using MediatR;

namespace Orders.Application.Customers.Commands.UpdateCustomer;

public sealed class UpdateCustomerCommandHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Customers.Customer), request.Id);

        var email = request.Email.Trim();

        if (!string.Equals(email, customer.Email, StringComparison.OrdinalIgnoreCase)
            && await repository.EmailExistsAsync(email, excludingId: request.Id, cancellationToken))
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        customer.UpdateDetails(request.Name, email, request.Phone);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
