namespace Inventory.Application.Common.Interfaces;

// Remembers the idempotency keys of requests that were already applied, so a repeated request (typically a
// retry after a lost response) is answered without being applied a second time.
public interface IIdempotencyStore
{
    Task<bool> HasProcessedAsync(string key, CancellationToken cancellationToken);

    // Recorded in the same transaction as the change it protects, and the key is unique in the database:
    // of two concurrent requests with the same key only one can commit.
    void MarkProcessed(string key);
}
