using Inventory.Application.Common.Interfaces;
using Inventory.Domain.ProductTypes;
using MediatR;

namespace Inventory.Application.ProductTypes.Commands.CreateProductType;

public sealed class CreateProductTypeCommandHandler(IProductTypeRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductTypeCommand, CreatedProductTypeDto>
{
    public async Task<CreatedProductTypeDto> Handle(CreateProductTypeCommand request, CancellationToken cancellationToken)
    {
        var productType = ProductType.Create(request.Name, request.Description);

        repository.Add(productType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedProductTypeDto.FromDomain(productType);
    }
}
