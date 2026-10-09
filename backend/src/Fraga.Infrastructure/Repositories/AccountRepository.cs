using Fraga.Application.Abstractions;
using Fraga.Domain.Entities;
using Fraga.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fraga.Infrastructure.Repositories;

/**
 * Implementa as operações relacionadas ao acesso
 * aos dados de contas.
 */
public sealed class AccountRepository(AppDbContext context) : IAccountRepository
{
    /**
     * Retorna todas as contas.
     */
    public async Task<IReadOnlyList<Account>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    /**
     * Retorna uma conta pelo ID do evento.
     */
    public Task<Account?> GetByEventIdAsync(
        Guid accountId, 
        CancellationToken cancellationToken)
    {
        return context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == accountId, cancellationToken);
    }

    /**
     * Retorna o extrato de uma conta.
     */
    public async Task<IReadOnlyList<Transaction>> GetStatementAsync(
        Guid accountId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.OccurredAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /**
     * Retorna o extrato de uma conta.
     */
    public async Task<int> CountStatementAsync(
        Guid accountId, 
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .CountAsync(cancellationToken);
    }
}