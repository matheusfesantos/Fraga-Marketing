using Fraga.Application.Abstractions;
using Fraga.Domain.Entities;
using Fraga.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fraga.Infrastructure.Repositories;

/**
 * Implementa as operações de persistência de dados
 * relacionadas a transações financeiras.
 */
public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    /**
     * Verifica se uma transação com o EventId informado
     * já foi processada.
     */
    public Task<bool> ExistsByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return _context.Transactions
            .AnyAsync(transaction => transaction.EventId == eventId, cancellationToken);
    }

    /**
     * Recupera uma conta para atualização, bloqueando-a para evitar concorrência.
     */
    public Task<Account?> GetAccountForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return _context.Accounts
            .FromSqlInterpolated(
                $"SELECT * FROM accounts WHERE \"Id\" = {accountId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /**
     * Adiciona uma nova transação ao contexto.
     */
    public async Task AddTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    /**
     * Salva as alterações no contexto.
     */
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    /**
     * Executa uma operação de forma atômica, dentro de uma transação.
     */
    public async Task ExecuteAtomicAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}