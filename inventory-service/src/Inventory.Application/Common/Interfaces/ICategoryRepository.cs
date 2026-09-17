using Inventory.Domain.Categories;

namespace Inventory.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);

    void Add(Category category);

    void Remove(Category category);
}
