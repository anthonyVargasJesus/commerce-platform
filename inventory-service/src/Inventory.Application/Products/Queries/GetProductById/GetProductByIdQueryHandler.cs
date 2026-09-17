using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Products.Product), request.Id);

        return ProductDto.FromDomain(product);
    }
}
