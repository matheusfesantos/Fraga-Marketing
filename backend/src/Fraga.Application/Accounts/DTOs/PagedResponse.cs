namespace Fraga.Application.Accounts.DTOs;

/**
 * Representa uma resposta paginada.
 */
public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);