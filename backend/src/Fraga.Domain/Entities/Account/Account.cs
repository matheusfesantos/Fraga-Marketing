using Fraga.Domain.Exceptions;

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

    /**
     * Realiza o crédito de um valor na conta.
     *
     * @throws ArgumentOutOfRangeException Se o valor não for maior que zero.
     */
    public void Credit(decimal amount)
    {
        EnsurePositive(amount);
        Balance += amount;
    }

    public bool CanDebit(decimal amount)
    {
        return Balance >= amount;
    }

    /**
     * Realiza o débito de um valor da conta.
     *
     * @param amount O valor a ser debitado.
     *
     * @throws ArgumentOutOfRangeException Se o valor não for maior que zero.
     * @throws InsufficientBalanceException Se o saldo da conta for insuficiente
     * para realizar o débito.
     */
    public void Debit(decimal amount)
    {
        EnsurePositive(amount);

        if (!CanDebit(amount))
            throw new InsufficientBalanceException();

        Balance -= amount;
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "O valor da transação deve ser maior que zero.");
    }
}