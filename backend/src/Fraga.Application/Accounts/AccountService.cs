using Fraga.Application.Abstractions;
using Fraga.Application.Accounts.DTOs;
using Fraga.Domain.Entities;

namespace Fraga.Application.Accounts;

/**
 * Serviço responsável pelas operações relacionadas a contas.
 */
public sealed class AccountService(
    IAccountRepository repository,
    ILogService logService)
{
    /**
     * Cria uma nova conta com saldo zero.
     *
     * @param request Dados da conta a ser criada.
     * @param cancellationToken Token de cancelamento para operações assíncronas.
     * @return A conta criada.
     * @throws ArgumentException Se o nome da conta for inválido.
     */
    public async Task<AccountResponse> CreateAsync(
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A validação do nome é feita pelo domínio.
        var account = new Account(Guid.NewGuid(), request.Name);

        await repository.AddAsync(account, cancellationToken);

        logService.Information(
            "Conta criada. AccountId: {AccountId}",
            account.Id);

        return ToResponse(account);
    }

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

        var responses = accounts
            .Select(ToResponse)
            .ToList();

        logService.Information(
            "Consulta de contas concluída. Count: {AccountCount}",
            responses.Count);

        return responses;
    }

    /**
     * Obtém uma conta pelo seu ID.
     *
     * @param accountId O ID da conta.
     * @param cancellationToken Token de cancelamento para operações assíncronas.
     * @return Uma resposta de conta ou nula se a conta não for encontrada.
     * @throws ArgumentException Se o ID da conta for vazio.
     */
    public async Task<AccountResponse?> GetByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException(
                "O ID da conta não pode ser vazio.",
                nameof(accountId));

        var account = await repository.GetByIdAsync(
            accountId,
            cancellationToken);

        logService.Information(
            account is null
                ? "Consulta de conta concluída: conta não encontrada. AccountId: {AccountId}"
                : "Consulta de conta concluída. AccountId: {AccountId}",
            accountId);

        return account is null
            ? null
            : ToResponse(account);
    }

    /**
     * Obtém o extrato de transações de uma conta.
     *
     * @param accountId O ID da conta.
     * @param page O número da página a ser obtida.
     * @param pageSize O tamanho da página.
     * @param cancellationToken Token de cancelamento para operações assíncronas.
     * @return Uma resposta paginada contendo os itens do extrato de transações.
     * @throws ArgumentException Se o ID da conta for vazio.
     * @throws ArgumentOutOfRangeException Se o número da página ou tamanho da página forem inválidos.
     * @throws KeyNotFoundException Se a conta não for encontrada.
     */
    public async Task<PagedResponse<TransactionStatementItemResponse>> GetStatementAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException(
                "O ID da conta não pode ser vazio.",
                nameof(accountId));

        if (page < 1)
            throw new ArgumentOutOfRangeException(
                nameof(page), "O número da página deve ser maior ou igual a 1.");

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize), "O tamanho da página deve estar entre 1 e 100.");

        var account = await repository.GetByIdAsync(
            accountId,
            cancellationToken);

        if (account is null)
        {
            logService.Warning(
                "Consulta de extrato rejeitada: conta não encontrada. AccountId: {AccountId}",
                accountId);

            throw new KeyNotFoundException("Conta não encontrada.");
        }

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
                transaction.OccurredAt,
                transaction.BalanceAfter
            )
        ).ToList();

        var totalPages = CalculateTotalPages(totalCount, pageSize);

        logService.Information(
            "Consulta de extrato concluída. AccountId: {AccountId}, Page: {Page}, PageSize: {PageSize}, ItemCount: {ItemCount}, TotalCount: {TotalCount}",
            accountId,
            page,
            pageSize,
            items.Count,
            totalCount);

        return new PagedResponse<TransactionStatementItemResponse>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages
        );
    }

    private static AccountResponse ToResponse(Account account)
    {
        return new AccountResponse(account.Id, account.Name, account.Balance);
    }

    /**
     * Calcula o número total de páginas com base no total de itens e no tamanho da página.
     *
     * @param totalCount O número total de itens.
     * @param pageSize O tamanho da página.
     * @return O número total de páginas.
     * @throws ArgumentOutOfRangeException Se o total de itens for negativo ou o tamanho da página for menor ou igual a zero.
     */
    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return (int)Math.Ceiling((double)totalCount / pageSize);
    }
}