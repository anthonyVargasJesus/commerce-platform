using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Commands.AdjustStock;

public sealed class AdjustStockCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Products.Product), request.ProductId);

        product.AdjustStock(request.Delta);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductDto.FromDomain(product);
    }
}
