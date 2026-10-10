namespace Fraga.Application.Transactions.DTOs;

/**
 * Representa o resultado do processamento de um evento financeiro.
 */
public sealed record ProcessTransactionResponse(
    Guid TransactionId,
    Guid EventId,
    Guid AccountId,
    decimal Balance
);