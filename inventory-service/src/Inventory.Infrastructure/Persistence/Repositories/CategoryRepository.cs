using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class CategoryRepository(InventoryDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);

    public void Remove(Category category) => dbContext.Categories.Remove(category);
}
