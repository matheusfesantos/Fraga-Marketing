namespace Fraga.Domain.Exceptions;

/**
 * Indica que um evento financeiro com o mesmo EventId
 * já foi processado anteriormente.
 */
public sealed class DuplicateEventException : DomainException
{
    public DuplicateEventException()
        : base("O evento informado já foi processado.")
    {
    }

    public DuplicateEventException(Exception innerException)
        : base("O evento informado já foi processado.", innerException)
    {
    }
}