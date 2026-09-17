using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.ProductTypes.Commands.DeleteProductType;

public sealed class DeleteProductTypeCommandHandler(
    IProductTypeRepository productTypeRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductTypeCommand>
{
    public async Task Handle(DeleteProductTypeCommand request, CancellationToken cancellationToken)
    {
        var productType = await productTypeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.ProductTypes.ProductType), request.Id);

        if (await productRepository.ExistsForProductTypeAsync(request.Id, cancellationToken))
        {
            throw new ConflictException($"Product type '{productType.Name}' cannot be deleted because it still has products assigned to it.");
        }

        productTypeRepository.Remove(productType);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
