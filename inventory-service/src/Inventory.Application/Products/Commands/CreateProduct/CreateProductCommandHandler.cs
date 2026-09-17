using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Products.Dtos;
using Inventory.Domain.Products;
using MediatR;

namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository repository,
    ICategoryRepository categoryRepository,
    IProductTypeRepository productTypeRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await repository.SkuExistsAsync(request.Sku, cancellationToken))
        {
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");
        }

        if (request.CategoryId is not null && await categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(Domain.Categories.Category), request.CategoryId.Value);
        }

        if (request.ProductTypeId is not null && await productTypeRepository.GetByIdAsync(request.ProductTypeId.Value, cancellationToken) is null)
        {
            throw new NotFoundException(nameof(Domain.ProductTypes.ProductType), request.ProductTypeId.Value);
        }

        var product = Product.Create(
            request.Sku,
            request.Name,
            request.Price,
            request.InitialQuantity,
            request.ReorderLevel,
            request.Description,
            request.CategoryId,
            request.ProductTypeId);

        repository.Add(product);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductDto.FromDomain(product);
    }
}
