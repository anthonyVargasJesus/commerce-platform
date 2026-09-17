using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository repository,
    ICategoryRepository categoryRepository,
    IProductTypeRepository productTypeRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Products.Product), request.Id);

        if (request.CategoryId is not null && await categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(Domain.Categories.Category), request.CategoryId.Value);
        }

        if (request.ProductTypeId is not null && await productTypeRepository.GetByIdAsync(request.ProductTypeId.Value, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(Domain.ProductTypes.ProductType), request.ProductTypeId.Value);
        }

        product.UpdateDetails(request.Name, request.Description, request.Price, request.ReorderLevel);
        product.AssignCategory(request.CategoryId);
        product.AssignProductType(request.ProductTypeId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
