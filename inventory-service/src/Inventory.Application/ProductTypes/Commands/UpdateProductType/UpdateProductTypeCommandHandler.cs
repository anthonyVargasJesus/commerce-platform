using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.ProductTypes.Commands.UpdateProductType;

public sealed class UpdateProductTypeCommandHandler(IProductTypeRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductTypeCommand>
{
    public async Task Handle(UpdateProductTypeCommand request, CancellationToken cancellationToken)
    {
        var productType = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.ProductTypes.ProductType), request.Id);

        productType.UpdateDetails(request.Name, request.Description);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
