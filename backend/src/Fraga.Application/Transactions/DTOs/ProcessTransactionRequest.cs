using Fraga.Domain.Enums;

namespace Fraga.Application.Transactions.DTOs;

public sealed record ProcessTransactionRequest(
    Guid EventId,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    DateTimeOffset OccurredAt);