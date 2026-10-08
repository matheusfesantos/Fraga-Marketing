namespace Fraga.Application.Authentication.DTOs;

/**
 * Dados retornados após o login.
 */
public record LoginResponse(
    string Token,
    DateTime ExpiresAt
);