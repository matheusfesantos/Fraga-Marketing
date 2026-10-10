using Fraga.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Fraga.Tests.Integration;

/**
 * Sobe um PostgreSQL real em container, uma única vez por classe de teste,
 * e aplica as migrations do projeto.
 */
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CriarContexto();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /**
     * Cada operação concorrente precisa do seu próprio contexto,
     * pois o DbContext não é thread-safe.
     */
    public AppDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }
}