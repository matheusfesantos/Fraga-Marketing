using System.Transactions;

namespace Fraga.Domain.Entities;
/**
 * Representa uma conta bancária dentro do domínio da aplicação.
 *
 * A conta mantém seu saldo atual e o relacionamento
 * com seu histórico de transações.
 */
public class Account
{
    public Guid Id { get; private set; }

    public decimal Balance { get; private set; }

    public ICollection<Transaction> Transactions { get; private set; } =
        new List<Transaction>();

    private Account()
    {
    }

    public Account(Guid id)
    {
        Id = id;
        Balance = 0;
    }

    public void Credit(decimal amount)
    {
        Balance += amount;
    }

    public bool CanDebit(decimal amount)
    {
        return Balance >= amount;
    }

    public void Debit(decimal amount)
    {
        if (!CanDebit(amount))
            throw new InvalidOperationException("Insufficient balance.");

        Balance -= amount;
    }
}