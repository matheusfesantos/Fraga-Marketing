namespace Fraga.Application.Accounts.DTOs;

/**
 * Representa a resposta de uma conta.
 */
public sealed record AccountResponse(
    Guid Id,
    string name,
    decimal Balance
);