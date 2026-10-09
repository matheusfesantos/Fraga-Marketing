using Fraga.Application.Abstractions;
using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;

namespace Fraga.Application.Transactions;

/**
 * Implementa as regras necessárias para processar
 * eventos financeiros de crédito e débito.
 */
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
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
        if (request.Amount <= 0)
            throw new ArgumentException("O valor da transação deve ser maior que zero.");

        await _repository.ExecuteAtomicAsync(async cancellationToken =>
        {
            var transactionAlreadyExists = await _repository.ExistsByEventIdAsync(
                request.EventId,
                cancellationToken);

            if (transactionAlreadyExists)
                throw new InvalidOperationException(
                    "O evento informado já foi processado.");

            var account = await _repository.GetAccountForUpdateAsync(
                request.AccountId,
                cancellationToken);

            if (account is null)
                throw new KeyNotFoundException(
                    "A conta informada não foi encontrada.");

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
                request.OccurredAt);

            await _repository.AddTransactionAsync(
                transaction,
                cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        });
    }
}
