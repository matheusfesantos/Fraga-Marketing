using Fraga.Application.Abstractions;
using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;
using Fraga.Domain.Exceptions;

namespace Fraga.Application.Transactions;

/**
 * Implementa as regras necessárias para processar
 * eventos financeiros de crédito e débito.
 */
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly ILogService _logService;

    public TransactionService(
        ITransactionRepository repository,
        ILogService logService)
    {
        _repository = repository;
        _logService = logService;
    }

    /**
     * Processa um evento financeiro garantindo:
     *
     * - idempotência por EventId;
     * - existência da conta;
     * - saldo suficiente para débitos;
     * - atualização do saldo;
     * - registro da transação;
     * - execução atômica da operação.
     */
    public async Task ProcessAsync(ProcessTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        if (request.Amount <= 0)
            throw new ArgumentException("O valor da transação deve ser maior que zero.");

        await _repository.ExecuteAtomicAsync(async cancellationToken =>
        {
            var account = await _repository.GetAccountForUpdateAsync(
                request.AccountId,
                cancellationToken);

            if (account is null)
            {
                _logService.Warning(
                    "Transação rejeitada: conta não encontrada. EventId: {EventId}, AccountId: {AccountId}",
                    request.EventId,
                    request.AccountId);

                throw new KeyNotFoundException(
                    "A conta informada não foi encontrada.");
            }

            var transactionAlreadyExists = await _repository.ExistsByEventIdAsync(
                request.EventId,
                cancellationToken);

            if (transactionAlreadyExists)
            {
                _logService.Warning(
                    "Transação rejeitada por evento duplicado. EventId: {EventId}",
                    request.EventId);

                throw new DuplicateEventException();
            }

            if (request.Type == Domain.Enums.TransactionType.Credit)
            {
                account.Credit(request.Amount);
            }
            else
            {
                account.Debit(request.Amount);
            }

            var transaction = new Transaction(
                request.EventId,
                request.AccountId,
                request.Type,
                request.Amount,
                request.OccurredAt,
                account.Balance);

            await _repository.AddTransactionAsync(
                transaction,
                cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        });

        _logService.Information(
            "Transação processada. EventId: {EventId}, AccountId: {AccountId}, Type: {TransactionType}",
            request.EventId,
            request.AccountId,
            request.Type);
    }

    private static void ValidateRequest(ProcessTransactionRequest request)
    {
        if (request.EventId == Guid.Empty)
            throw new ArgumentException(
                "O identificador do evento não pode ser vazio.");

        if (request.AccountId == Guid.Empty)
            throw new ArgumentException(
                "O identificador da conta não pode ser vazio.");

        if (!Enum.IsDefined(request.Type))
            throw new ArgumentException(
                "O tipo da transação é inválido.");

        if (request.Amount <= 0)
            throw new ArgumentException(
                "O valor da transação deve ser maior que zero.");
    }
}