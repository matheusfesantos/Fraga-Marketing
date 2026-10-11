namespace Fraga.Domain.Exceptions;

/**
 * Indica que a conta não possui saldo suficiente
 * para realizar o débito solicitado.
 */
public sealed class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException()
        : base("Saldo insuficiente para realizar o débito.")
    {
    }
}