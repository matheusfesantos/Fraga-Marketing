using Fraga.Domain.Entities;

namespace Fraga.Application.Abstractions;

/**
 * Define as operações relacionadas ao acesso
 * aos dados de contas.
 */
public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Account?> GetByEventIdAsync(
        Guid accountId, 
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetStatementAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<int> CountStatementAsync(
        Guid accountId,
        CancellationToken cancellationToken
    );
}