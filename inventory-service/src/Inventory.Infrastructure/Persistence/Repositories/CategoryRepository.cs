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

    public Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(c => c.Name == name && (excludingId == null || c.Id != excludingId), cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);

    public void Remove(Category category) => dbContext.Categories.Remove(category);
}
