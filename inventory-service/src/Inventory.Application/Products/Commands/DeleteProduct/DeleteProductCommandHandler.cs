using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Products.Product), request.Id);

        product.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
