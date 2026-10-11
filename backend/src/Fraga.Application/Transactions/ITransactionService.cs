using Fraga.Application.Transactions.DTOs;

namespace Fraga.Application.Transactions;

/**
 * Define as operações relacionadas ao processamento
 * de eventos financeiros.
 */
public interface ITransactionService
{
    /**
     * Processa um evento financeiro, atualiza o saldo e o histórico
     * da conta e devolve o saldo resultante.
     */
    Task<ProcessTransactionResponse> ProcessAsync(
        ProcessTransactionRequest request,
        CancellationToken cancellationToken = default);
}