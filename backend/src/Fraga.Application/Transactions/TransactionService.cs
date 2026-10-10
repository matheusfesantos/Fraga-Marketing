using Fraga.Application.Abstractions;
using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;
using Fraga.Domain.Enums;
using Fraga.Domain.Exceptions;

namespace Fraga.Application.Transactions;

/**
 * Implementa as regras necessárias para processar
 * eventos financeiros de crédito e débito.
 */
public class TransactionService : ITransactionService
{
    private const int AmountDecimalPlaces = 2;

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
    public async Task<ProcessTransactionResponse> ProcessAsync(
        ProcessTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        ProcessTransactionResponse? response = null;

        await _repository.ExecuteAtomicAsync(async token =>
        {
            var account = await _repository.GetAccountForUpdateAsync(
                request.AccountId,
                token);

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
                token);

            if (transactionAlreadyExists)
            {
                _logService.Warning(
                    "Transação rejeitada por evento duplicado. EventId: {EventId}",
                    request.EventId);

                throw new DuplicateEventException();
            }

            if (request.Type == TransactionType.Credit)
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

            await _repository.AddTransactionAsync(transaction, token);
            await _repository.SaveChangesAsync(token);

            response = new ProcessTransactionResponse(
                transaction.Id,
                transaction.EventId,
                transaction.AccountId,
                account.Balance);
        }, cancellationToken);

        _logService.Information(
            "Transação processada. EventId: {EventId}, AccountId: {AccountId}, Type: {TransactionType}",
            request.EventId,
            request.AccountId,
            request.Type);

        return response!;
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

        // A coluna é numeric(18,2): mais casas seriam arredondadas em silêncio
        // e o histórico deixaria de refletir o que foi enviado.
        if (decimal.Round(request.Amount, AmountDecimalPlaces) != request.Amount)
            throw new ArgumentException(
                "O valor da transação deve ter no máximo duas casas decimais.");
    }
}