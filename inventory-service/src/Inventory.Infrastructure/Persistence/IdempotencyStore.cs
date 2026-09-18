using Inventory.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class IdempotencyStore(InventoryDbContext dbContext) : IIdempotencyStore
{
    public Task<bool> HasProcessedAsync(string key, CancellationToken cancellationToken) =>
        dbContext.ProcessedRequests.AnyAsync(request => request.Key == key, cancellationToken);

    public void MarkProcessed(string key) =>
        dbContext.ProcessedRequests.Add(new ProcessedRequest { Key = key, ProcessedAt = DateTimeOffset.UtcNow });
}
