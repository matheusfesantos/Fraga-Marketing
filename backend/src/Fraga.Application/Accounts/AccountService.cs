using Fraga.Application.Accounts.DTOs;
using Fraga.Application.Abstractions;

namespace Fraga.Application.Accounts;

/**
 * Serviço responsável pelas operações relacionadas a contas.
 */
public sealed class AccountService(IAccountRepository repository)
{
    /**
     * Obtém todas as contas.
     *
     * @param cancellationToken Token de cancelamento para operações assíncronas.
     * @return Uma lista de respostas de contas.
     */
    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await repository.GetAllAsync(cancellationToken);

        return accounts
           .Select(account => new AccountResponse(
                account.Id,
                account.Balance
            ))
            .ToList();
    }

    /**
     * Obtém uma conta pelo seu ID.
     *
     * @param accountId O ID da conta a ser obtida.
     * @param cancellationToken Token de cancelamento para operações assíncronas.
     * @return A resposta da conta ou nulo se não encontrada.
     * @throws ArgumentException Se o ID da conta for vazio.
     */
    public async Task<AccountResponse?> GetByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("O ID da conta não pode ser vazio.");

        var account = await repository.GetByEventIdAsync(
            accountId,
            cancellationToken);

        return account is null
            ? null
            : new AccountResponse(account.Id, account.Balance);
    }

    public async Task<PagedResponse<TransactionStatementItemResponse>> GetStatementAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("O ID da conta não pode ser vazio.");

        if (page < 1)
            throw new ArgumentOutOfRangeException(
                nameof(page), "O número da página deve ser maior ou igual a 1.");

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize), "O tamanho da página deve estar entre 1 e 100.");

        var account = await repository.GetByEventIdAsync(
            accountId,
            cancellationToken);

        if (account is null)
            throw new KeyNotFoundException("Conta não encontrada.");

        var transactions = await repository.GetStatementAsync(
            accountId,
            page,
            pageSize,
            cancellationToken);

        var totalCount = await repository.CountStatementAsync(
            accountId,
            cancellationToken);

        var items = transactions.Select(transaction =>
            new TransactionStatementItemResponse(
                transaction.Id,
                transaction.EventId,
                transaction.AccountId,
                transaction.Type,
                transaction.Amount,
                transaction.OccurredAt
            )
        ).ToList();

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "O tamanho da página deve ser maior que zero.");
        }

        var totalPages = CalculateTotalPages(totalCount, pageSize);

        return new PagedResponse<TransactionStatementItemResponse>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages
        );
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return (int)Math.Ceiling((double)totalCount / pageSize);
    }
}