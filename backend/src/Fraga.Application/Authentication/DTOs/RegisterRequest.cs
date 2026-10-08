namespace Fraga.Application.Authentication.DTOs;

/**
 * Dados necessários para criar um novo usuário.
 */
public record RegisterRequest(
    string Name,
    string Email,
    string Password
);