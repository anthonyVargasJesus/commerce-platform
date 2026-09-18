using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Commands.AdjustStock;

public sealed class AdjustStockCommandHandler(IProductRepository repository, IIdempotencyStore idempotencyStore, IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Products.Product), request.ProductId);

        if (request.IdempotencyKey is not null && await idempotencyStore.HasProcessedAsync(request.IdempotencyKey, cancellationToken))
        {
            // The same request was already applied (a retry after a lost response): answer as before, apply nothing.
            return ProductDto.FromDomain(product);
        }

        product.AdjustStock(request.Delta);

        if (request.IdempotencyKey is not null)
        {
            idempotencyStore.MarkProcessed(request.IdempotencyKey);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductDto.FromDomain(product);
    }
}
