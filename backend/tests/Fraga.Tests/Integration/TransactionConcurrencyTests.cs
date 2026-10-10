using Fraga.Application.Abstractions;
using Fraga.Application.Transactions;
using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;
using Fraga.Domain.Enums;
using Fraga.Domain.Exceptions;
using Fraga.Infrastructure.Data;
using Fraga.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Fraga.Tests.Integration;

/**
 * Testes de integração que validam, contra um PostgreSQL real,
 * idempotência, consistência e transacionalidade sob concorrência.
 */
[Trait("Category", "Integration")]
public sealed class TransactionConcurrencyTests : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset DataEvento =
        new(2026, 1, 30, 13, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _fixture;

    public TransactionConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /**
     * Várias requisições com o mesmo EventId para a mesma conta:
     * apenas uma pode ser processada.
     */
    [Fact(DisplayName = "Deve processar apenas uma vez o mesmo evento enviado em paralelo")]
    public async Task MesmoEvento_MesmaConta_EmParalelo_ProcessaUmaVez()
    {
        var contaId = await CriarContaAsync();
        var eventId = Guid.NewGuid();

        var requisicoes = Enumerable.Range(0, 20)
            .Select(_ => Requisicao(eventId, contaId, TransactionType.Credit, 100m))
            .ToList();

        var resultados = await ExecutarEmParaleloAsync(requisicoes);

        Assert.Equal(1, resultados.Count(r => r is null));
        Assert.Equal(19, resultados.Count(r => r is DuplicateEventException));

        var conta = await ConsultarAsync(contaId);

        Assert.Equal(100m, conta.Saldo);
        Assert.Single(conta.Transacoes);
    }

    /**
     * Mesmo EventId para contas diferentes: o lock de conta não protege,
     * quem garante a idempotência é o índice único do banco.
     */
    [Fact(DisplayName = "Deve processar apenas uma vez o mesmo evento enviado para contas diferentes")]
    public async Task MesmoEvento_ContasDiferentes_EmParalelo_ProcessaUmaVez()
    {
        var contaA = await CriarContaAsync();
        var contaB = await CriarContaAsync();
        var eventId = Guid.NewGuid();

        var requisicoes = Enumerable.Range(0, 10)
            .Select(i => Requisicao(
                eventId,
                i % 2 == 0 ? contaA : contaB,
                TransactionType.Credit,
                100m))
            .ToList();

        var resultados = await ExecutarEmParaleloAsync(requisicoes);

        Assert.Equal(1, resultados.Count(r => r is null));
        Assert.Equal(9, resultados.Count(r => r is DuplicateEventException));

        var a = await ConsultarAsync(contaA);
        var b = await ConsultarAsync(contaB);

        Assert.Equal(100m, a.Saldo + b.Saldo);
        Assert.Equal(1, a.Transacoes.Count + b.Transacoes.Count);
    }

    /**
     * Débitos simultâneos que, somados, excedem o saldo:
     * o saldo nunca pode ficar negativo.
     */
    [Fact(DisplayName = "Não deve deixar o saldo negativo com débitos concorrentes")]
    public async Task DebitosConcorrentes_SaldoNuncaFicaNegativo()
    {
        var contaId = await CriarContaAsync(saldoInicial: 100m);

        var requisicoes = Enumerable.Range(0, 10)
            .Select(_ => Requisicao(Guid.NewGuid(), contaId, TransactionType.Debit, 30m))
            .ToList();

        var resultados = await ExecutarEmParaleloAsync(requisicoes);

        Assert.Equal(3, resultados.Count(r => r is null));
        Assert.Equal(7, resultados.Count(r => r is InsufficientBalanceException));

        var conta = await ConsultarAsync(contaId);

        Assert.Equal(10m, conta.Saldo);
        Assert.Equal(4, conta.Transacoes.Count); // 1 crédito inicial + 3 débitos
        Assert.All(conta.Transacoes, t => Assert.True(t.BalanceAfter >= 0));
        Assert.Equal(conta.Saldo, SaldoDoHistorico(conta.Transacoes));
    }

    /**
     * Créditos simultâneos com EventIds distintos: nenhuma atualização
     * pode ser perdida, e o saldo deve refletir exatamente o histórico.
     */
    [Fact(DisplayName = "Não deve perder atualizações com créditos concorrentes")]
    public async Task CreditosConcorrentes_NaoPerdemAtualizacoes()
    {
        var contaId = await CriarContaAsync();

        var requisicoes = Enumerable.Range(0, 20)
            .Select(_ => Requisicao(Guid.NewGuid(), contaId, TransactionType.Credit, 10m))
            .ToList();

        var resultados = await ExecutarEmParaleloAsync(requisicoes);

        Assert.All(resultados, r => Assert.Null(r));

        var conta = await ConsultarAsync(contaId);

        Assert.Equal(200m, conta.Saldo);
        Assert.Equal(20, conta.Transacoes.Count);
        Assert.Equal(conta.Saldo, SaldoDoHistorico(conta.Transacoes));

        // Com o lock da conta, as operações são serializadas:
        // cada transação enxerga um saldo diferente (10, 20, ..., 200).
        var saldosEsperados = Enumerable.Range(1, 20).Select(i => i * 10m);
        var saldosObtidos = conta.Transacoes.Select(t => t.BalanceAfter).OrderBy(s => s);

        Assert.Equal(saldosEsperados, saldosObtidos);
    }

    /**
     * Débito rejeitado por saldo insuficiente não pode deixar rastro:
     * nem transação gravada, nem saldo alterado.
     */
    [Fact(DisplayName = "Não deve gravar nada quando o débito é rejeitado")]
    public async Task DebitoRejeitado_NaoGravaNada()
    {
        var contaId = await CriarContaAsync(saldoInicial: 50m);

        var resultado = await ProcessarAsync(
            Requisicao(Guid.NewGuid(), contaId, TransactionType.Debit, 100m));

        Assert.IsType<InsufficientBalanceException>(resultado);

        var conta = await ConsultarAsync(contaId);

        Assert.Equal(50m, conta.Saldo);
        Assert.Single(conta.Transacoes); // apenas o crédito inicial
    }

    // ---------- helpers ----------

    private static ProcessTransactionRequest Requisicao(
        Guid eventId,
        Guid contaId,
        TransactionType tipo,
        decimal valor)
    {
        return new ProcessTransactionRequest(eventId, contaId, tipo, valor, DataEvento);
    }

    /**
     * Cria uma conta e, se necessário, aplica o saldo inicial por um crédito
     * real, mantendo o saldo coerente com o histórico.
     */
    private async Task<Guid> CriarContaAsync(decimal saldoInicial = 0m)
    {
        var contaId = Guid.NewGuid();

        await using (var context = _fixture.CriarContexto())
        {
            context.Accounts.Add(new Account(contaId));
            await context.SaveChangesAsync();
        }

        if (saldoInicial > 0)
        {
            var erro = await ProcessarAsync(
                Requisicao(Guid.NewGuid(), contaId, TransactionType.Credit, saldoInicial));

            Assert.Null(erro);
        }

        return contaId;
    }

    /**
     * Processa uma requisição com um contexto próprio e devolve a exceção
     * lançada (ou null, se deu certo).
     */
    private async Task<Exception?> ProcessarAsync(ProcessTransactionRequest requisicao)
    {
        await using var context = _fixture.CriarContexto();

        var servico = new TransactionService(
            new TransactionRepository(context),
            Mock.Of<ILogService>());

        try
        {
            await servico.ProcessAsync(requisicao);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    /**
     * Dispara todas as requisições ao mesmo tempo, liberando-as por uma
     * "largada" única para maximizar a contenção.
     */
    private async Task<List<Exception?>> ExecutarEmParaleloAsync(
        IEnumerable<ProcessTransactionRequest> requisicoes)
    {
        var largada = new TaskCompletionSource();

        var tarefas = requisicoes
            .Select(requisicao => Task.Run(async () =>
            {
                await largada.Task;
                return await ProcessarAsync(requisicao);
            }))
            .ToList();

        largada.SetResult();

        return (await Task.WhenAll(tarefas)).ToList();
    }

    private async Task<(decimal Saldo, List<Transaction> Transacoes)> ConsultarAsync(Guid contaId)
    {
        await using var context = _fixture.CriarContexto();

        var conta = await context.Accounts
            .AsNoTracking()
            .SingleAsync(a => a.Id == contaId);

        var transacoes = await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == contaId)
            .ToListAsync();

        return (conta.Balance, transacoes);
    }

    private static decimal SaldoDoHistorico(IEnumerable<Transaction> transacoes)
    {
        return transacoes.Sum(t => t.Type == TransactionType.Credit ? t.Amount : -t.Amount);
    }
}