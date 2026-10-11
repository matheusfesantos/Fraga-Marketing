namespace Fraga.Application.Accounts.DTOs;

/**
 * Representa os dados necessários para criar uma conta.
 */
public sealed record CreateAccountRequest(
    string Name
);