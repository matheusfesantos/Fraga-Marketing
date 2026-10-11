using Fraga.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fraga.Infrastructure.Data;

/**
 * Prepara o banco na inicialização da aplicação:
 * aplica as migrations pendentes e cadastra contas iniciais
 * quando a base está vazia.
 */
public static class DbInitializer
{
    private static readonly (Guid Id, string Name)[] SeedAccounts =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Conta Corrente"),
        (Guid.Parse("22222222-2222-2222-2222-222222222222"), "Conta Poupança"),
        (Guid.Parse("33333333-3333-3333-3333-333333333333"), "Conta Investimentos")
    ];

    public static async Task InitializeAsync(
        AppDbContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Accounts.AnyAsync(cancellationToken))
            return;

        var accounts = SeedAccounts.Select(seed => new Account(seed.Id, seed.Name));

        await context.Accounts.AddRangeAsync(accounts, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}