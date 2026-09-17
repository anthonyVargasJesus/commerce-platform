using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.ProductTypes.Queries.GetProductTypeById;

public sealed class GetProductTypeByIdQueryHandler(IProductTypeRepository repository)
    : IRequestHandler<GetProductTypeByIdQuery, ProductTypeDetailsDto>
{
    public async Task<ProductTypeDetailsDto> Handle(GetProductTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var productType = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.ProductTypes.ProductType), request.Id);

        return ProductTypeDetailsDto.FromDomain(productType);
    }
}
