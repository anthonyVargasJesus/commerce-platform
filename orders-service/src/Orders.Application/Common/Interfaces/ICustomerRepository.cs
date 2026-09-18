using Orders.Domain.Customers;

namespace Orders.Application.Common.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken);

    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? excludingId, CancellationToken cancellationToken);

    void Add(Customer customer);

    void Remove(Customer customer);
}
