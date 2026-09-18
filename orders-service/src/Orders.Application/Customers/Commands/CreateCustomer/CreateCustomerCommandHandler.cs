using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using Orders.Domain.Customers;
using MediatR;

namespace Orders.Application.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler(ICustomerRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCustomerCommand, CreatedCustomerDto>
{
    public async Task<CreatedCustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        if (await repository.EmailExistsAsync(email, excludingId: null, cancellationToken))
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        var customer = Customer.Create(request.Name, email, request.Phone);

        repository.Add(customer);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedCustomerDto.FromDomain(customer);
    }
}
