using Fraga.Domain.Enums;

namespace Fraga.Application.Transactions.DTOs;

/**
 * Representa os dados necessários para processar
 * um evento financeiro recebido pela API.
 */
public record ProcessTransactionRequest(
    Guid EventId,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    DateTime OccurredAt
);