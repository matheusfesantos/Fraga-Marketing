namespace Fraga.Domain.Exceptions;

/**
 * Base para todas as exceções de regra de negócio do domínio.
 * Permite que a camada de API identifique falhas de negócio
 * sem depender de tipos genéricos do .NET.
 */
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}