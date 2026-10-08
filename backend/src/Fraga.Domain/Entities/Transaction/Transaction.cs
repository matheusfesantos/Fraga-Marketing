using Fraga.Domain.Enums;

namespace Fraga.Domain.Entities;

/**
 * Representa uma transação financeira recebida pela aplicação.
 *
 * Cada transação está vinculada a uma conta e possui
 * informações necessárias para registrar o evento financeiro.
 */
public class Transaction
{
    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid AccountId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public Account Account { get; private set; } = null!;

    private Transaction()
    {
    }

    /**
     * Cria uma nova transação financeira.
     *
     * @param eventId Identificador único do evento.
     * @param accountId Identificador da conta.
     * @param type Tipo da transação (crédito ou débito).
     * @param amount Valor da transação.
     * @param occurredAt Data e hora em que a transação ocorreu.
     */
    public Transaction(
        Guid eventId,
        Guid accountId,
        TransactionType type,
        decimal amount,
        DateTime occurredAt)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        AccountId = accountId;
        Type = type;
        Amount = amount;
        OccurredAt = occurredAt;
    }
}