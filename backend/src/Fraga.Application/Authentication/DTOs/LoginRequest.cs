namespace Fraga.Application.Authentication.DTOs;

/**
 * Dados necessários para realizar login.
 */
public record LoginRequest(
    string Email,
    string Password
);