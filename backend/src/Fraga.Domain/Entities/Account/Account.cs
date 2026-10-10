using Fraga.Domain.Exceptions;

namespace Fraga.Domain.Entities;

/**
 * Representa uma conta bancária dentro do domínio da aplicação.
 *
 * A conta possui um nome, mantém seu saldo atual e o relacionamento
 * com seu histórico de transações.
 */
public class Account
{
    public const int NameMaxLength = 100;

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal Balance { get; private set; }

    public ICollection<Transaction> Transactions { get; private set; } =
        new List<Transaction>();

    private Account()
    {
    }

    /**
     * Cria uma nova conta com saldo zero.
     *
     * @param id Identificador da conta.
     * @param name Nome da conta (obrigatório, até 100 caracteres).
     *
     * @throws ArgumentException Se o id for vazio ou o nome for inválido.
     */
    public Account(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "O identificador da conta não pode ser vazio.");

        Id = id;
        Name = NormalizeName(name);
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

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome da conta não pode ser vazio.");

        var normalized = name.Trim();

        if (normalized.Length > NameMaxLength)
            throw new ArgumentException(
                $"O nome da conta deve ter no máximo {NameMaxLength} caracteres.");

        return normalized;
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "O valor da transação deve ser maior que zero.");
    }
}