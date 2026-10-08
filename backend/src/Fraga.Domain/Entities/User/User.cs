namespace Fraga.Domain.Entities;

/**
 * Representa o usuário responsável pela autenticação
 * e pela conta financeira associada.
 */
public class User
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    /**
     * Identifica a conta financeira pertencente ao usuário.
     */
    public Guid AccountId { get; private set; }

    public Account Account { get; private set; } = null!;

    private User()
    {
    }

    /**
     * Cria um novo usuário associado a uma conta financeira.
     */
    public User(
        Guid id,
        string name,
        string email,
        string passwordHash,
        Guid accountId)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        AccountId = accountId;
    }
}