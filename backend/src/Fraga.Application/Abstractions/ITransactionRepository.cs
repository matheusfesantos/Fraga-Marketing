using Fraga.Domain.Entities;

namespace Fraga.Application.Abstractions;

public interface ITransactionRepository
{
    Task<bool> ExistsByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
    Task<Account?> GetAccountForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
    Task AddTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteAtomicAsync(
        Func<CancellationToken, Task> operation, 
        CancellationToken cancellationToken = default);
}