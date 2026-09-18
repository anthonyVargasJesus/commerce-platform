using Orders.Application.Common.Exceptions;
using Orders.Application.Common.Interfaces;
using MediatR;

namespace Orders.Application.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler(ICustomerRepository repository)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDetailsDto>
{
    public async Task<CustomerDetailsDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Customers.Customer), request.Id);

        return CustomerDetailsDto.FromDomain(customer);
    }
}
