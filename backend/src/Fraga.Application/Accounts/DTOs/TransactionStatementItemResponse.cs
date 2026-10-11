using Fraga.Domain.Enums;

namespace Fraga.Application.Accounts.DTOs;

/**
 * Representa a resposta de um item de extrato de transação.
 */
public sealed record TransactionStatementItemResponse(
    Guid Id,
    Guid EventId,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    DateTimeOffset OccurredAt,
    decimal BalanceAfter
);