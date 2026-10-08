using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;
using Fraga.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fraga.Application.Transactions;

/**
 * Implementa as regras necessárias para processar
 * eventos financeiros de crédito e débito.
 */
public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    /**
     * Inicializa o serviço com o contexto do banco de dados.
     */
    public TransactionService(AppDbContext context)
    {
        _context = context;
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
        /**
         * Impede valores inválidos de serem processados.
         */
        if (request.Amount <= 0)
            throw new ArgumentException("O valor da transação deve ser maior que zero.");

        /**
         * Verifica se o evento já foi processado.
         *
         * O índice UNIQUE em EventId no banco também protege
         * contra duplicidade em situações concorrentes.
         */
        var transactionAlreadyExists = await _context.Transactions
            .AnyAsync(transaction => transaction.EventId == request.EventId);

        if (transactionAlreadyExists)
            throw new InvalidOperationException(
                "O evento informado já foi processado.");

        /**
         * Busca a conta que receberá a movimentação.
         */
        var account = await _context.Accounts
            .FirstOrDefaultAsync(account => account.Id == request.AccountId);

        if (account is null)
            throw new KeyNotFoundException(
                "A conta informada não foi encontrada.");

        /**
         * Inicia uma transação no banco.
         *
         * O saldo e o histórico serão persistidos juntos.
         */
        await using var databaseTransaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            /**
             * Aplica a movimentação no saldo da conta.
             */
            if (request.Type == Domain.Enums.TransactionType.Credit)
            {
                account.Credit(request.Amount);
            }
            else
            {
                account.Debit(request.Amount);
            }

            /**
             * Cria o registro permanente do evento financeiro.
             */
            var transaction = new Transaction(
                request.EventId,
                request.AccountId,
                request.Type,
                request.Amount,
                request.OccurredAt);

            await _context.Transactions.AddAsync(transaction);

            /**
             * Persiste saldo e histórico.
             */
            await _context.SaveChangesAsync();

            /**
             * Confirma a operação somente depois
             * que todas as alterações foram persistidas.
             */
            await databaseTransaction.CommitAsync();
        }
        catch
        {
            /**
             * Se qualquer operação falhar, nenhuma alteração
             * financeira deve permanecer no banco.
             */
            await databaseTransaction.RollbackAsync();

            throw;
        }
    }
}