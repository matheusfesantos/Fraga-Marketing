using Fraga.Application.Abstractions;
using Fraga.Application.Transactions;
using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;
using Fraga.Domain.Enums;
using Moq;

namespace Fraga.Tests.Transactions;

/**
 * Testes unitários complementares para a classe TransactionService:
 * validação de casas decimais, normalização para UTC, débito válido
 * e conteúdo da resposta.
 */
public sealed class TransactionServiceHardeningTests
{
    private readonly Mock<ITransactionRepository> _repositoryMock = new();
    private readonly Mock<ILogService> _logServiceMock = new();
    private readonly TransactionService _service;

    public TransactionServiceHardeningTests()
    {
        _service = new TransactionService(
            _repositoryMock.Object,
            _logServiceMock.Object);

        _repositoryMock
            .Setup(repository => repository.ExecuteAtomicAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<CancellationToken, Task> operation,
                CancellationToken cancellationToken)
                => operation(cancellationToken));
    }

    [Fact(DisplayName = "Deve rejeitar valor com mais de duas casas decimais")]
    public async Task Deve_Rejeitar_Valor_Com_Mais_De_Duas_Casas_Decimais()
    {
        var requisicao = new ProcessTransactionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Credit,
            10.555m,
            DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ProcessAsync(requisicao));

        Assert.Contains("duas casas decimais", exception.Message);

        _repositoryMock.Verify(
            repository => repository.ExecuteAtomicAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Deve reduzir o saldo e registrar a transação ao processar um débito válido")]
    public async Task Deve_Reduzir_Saldo_E_Registrar_Transacao_Ao_Processar_Debito_Valido()
    {
        var conta = new Account(Guid.NewGuid(), "Conta de teste");
        conta.Credit(100m);

        var requisicao = new ProcessTransactionRequest(
            Guid.NewGuid(),
            conta.Id,
            TransactionType.Debit,
            40.25m,
            DateTimeOffset.UtcNow);

        ConfigurarContaExistente(conta);

        Transaction? registrada = null;

        _repositoryMock
            .Setup(repository => repository.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transacao, _) => registrada = transacao)
            .Returns(Task.CompletedTask);

        await _service.ProcessAsync(requisicao);

        Assert.Equal(59.75m, conta.Balance);
        Assert.NotNull(registrada);
        Assert.Equal(TransactionType.Debit, registrada.Type);
        Assert.Equal(40.25m, registrada.Amount);
        Assert.Equal(59.75m, registrada.BalanceAfter);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Deve normalizar a data da transação para UTC")]
    public async Task Deve_Normalizar_Data_Da_Transacao_Para_Utc()
    {
        var conta = new Account(Guid.NewGuid(), "Conta de teste");

        var ocorridoEmBrasilia = new DateTimeOffset(
            2026, 1, 30, 10, 15, 0, TimeSpan.FromHours(-3));

        var requisicao = new ProcessTransactionRequest(
            Guid.NewGuid(),
            conta.Id,
            TransactionType.Credit,
            10m,
            ocorridoEmBrasilia);

        ConfigurarContaExistente(conta);

        Transaction? registrada = null;

        _repositoryMock
            .Setup(repository => repository.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transacao, _) => registrada = transacao)
            .Returns(Task.CompletedTask);

        await _service.ProcessAsync(requisicao);

        Assert.NotNull(registrada);
        Assert.Equal(TimeSpan.Zero, registrada.OccurredAt.Offset);
        Assert.Equal(ocorridoEmBrasilia.UtcDateTime, registrada.OccurredAt.UtcDateTime);
    }

    [Fact(DisplayName = "Deve devolver o saldo atualizado na resposta")]
    public async Task Deve_Devolver_Saldo_Atualizado_Na_Resposta()
    {
        var conta = new Account(Guid.NewGuid(), "Conta de teste");
        conta.Credit(20m);

        var requisicao = new ProcessTransactionRequest(
            Guid.NewGuid(),
            conta.Id,
            TransactionType.Credit,
            30m,
            DateTimeOffset.UtcNow);

        ConfigurarContaExistente(conta);

        var resposta = await _service.ProcessAsync(requisicao);

        Assert.Equal(50m, resposta.Balance);
        Assert.Equal(conta.Id, resposta.AccountId);
        Assert.Equal(requisicao.EventId, resposta.EventId);
        Assert.NotEqual(Guid.Empty, resposta.TransactionId);
    }

    private void ConfigurarContaExistente(Account conta)
    {
        _repositoryMock
            .Setup(repository => repository.GetAccountForUpdateAsync(
                conta.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        _repositoryMock
            .Setup(repository => repository.ExistsByEventIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }
}