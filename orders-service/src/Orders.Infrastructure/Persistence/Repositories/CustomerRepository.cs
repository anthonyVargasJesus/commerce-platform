using Orders.Application.Common.Interfaces;
using Orders.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence.Repositories;

public class CustomerRepository(OrdersDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Customers.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken) =>
        dbContext.Customers.AnyAsync(c => c.Email == email && (excludingId == null || c.Id != excludingId), cancellationToken);

    public void Add(Customer customer) => dbContext.Customers.Add(customer);

    public void Remove(Customer customer) => dbContext.Customers.Remove(customer);
}
