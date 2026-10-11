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

    Task<bool> ExistsAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Account?> GetByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default);

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