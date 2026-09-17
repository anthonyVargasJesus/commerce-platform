using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.ProductTypes;
using MediatR;

namespace Inventory.Application.ProductTypes.Commands.CreateProductType;

public sealed class CreateProductTypeCommandHandler(IProductTypeRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductTypeCommand, CreatedProductTypeDto>
{
    public async Task<CreatedProductTypeDto> Handle(CreateProductTypeCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await repository.NameExistsAsync(name, excludingId: null, cancellationToken))
        {
            throw new ConflictException($"A product type with name '{name}' already exists.");
        }

        var productType = ProductType.Create(name, request.Description);

        repository.Add(productType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedProductTypeDto.FromDomain(productType);
    }
}
