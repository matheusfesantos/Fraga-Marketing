using Fraga.Application.Transactions.DTOs;

namespace Fraga.Application.Transactions;

/**
 * Define as operações relacionadas ao processamento
 * de eventos financeiros.
 */
public interface ITransactionService
{
    /**
     * Processa um evento financeiro e atualiza
     * o saldo e o histórico da conta.
     */
    Task ProcessAsync(ProcessTransactionRequest request);
}