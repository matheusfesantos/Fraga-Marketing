using Fraga.Application.Abstractions;
using Fraga.Application.Transactions;
using Fraga.Application.Transactions.DTOs;
using Fraga.Domain.Entities;
using Fraga.Domain.Enums;
using Fraga.Domain.Exceptions;
using Moq;

namespace Fraga.Tests.Transactions;

/**
 * Testes unitários para a classe TransactionService.
 */
public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repositoryMock = new();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _service = new TransactionService(_repositoryMock.Object);

        _repositoryMock
            .Setup(repository => repository.ExecuteAtomicAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<CancellationToken, Task> operation,
                CancellationToken cancellationToken)
                => operation(cancellationToken));
    }

    /**
     * Testa o processamento de uma transação com valor zero.
     * Deve lançar uma exceção ArgumentException.
     */
    [Fact(DisplayName = "Deve lançar exceção quando o valor da transação for zero")]
    public async Task Deve_Lancar_Excecao_Quando_Valor_de_Transacao_for_Zero()
    {
        var requisicao = new ProcessTransactionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TransactionType.Credit,
                0,
                DateTime.UtcNow);

        var exception = await Assert.ThrowsAsync<ArgumentException>
        (() => _service.ProcessAsync(requisicao));

        Assert.Contains("maior que zero", exception.Message);

        _repositoryMock.Verify(
            repository => repository.ExecuteAtomicAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /**
     * Testa o processamento de um evento que já foi processado.
     * Deve lançar DuplicateEventException e não gravar nada.
     * A verificação de duplicidade ocorre após o bloqueio da conta.
     */
    [Fact(DisplayName = "Deve rejeitar evento que já foi processado")]
    public async Task Deve_Rejeitar_Evento_Que_Ja_Foi_Processado()
    {
        var conta = new Account(Guid.NewGuid());

        var requisicao = new ProcessTransactionRequest(
                Guid.NewGuid(),
                conta.Id,
                TransactionType.Credit,
                100m,
                DateTime.UtcNow);

        _repositoryMock
            .Setup(repository => repository.GetAccountForUpdateAsync(
                conta.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        _repositoryMock
            .Setup(repository => repository.ExistsByEventIdAsync(
                requisicao.EventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<DuplicateEventException>
        (() => _service.ProcessAsync(requisicao));

        Assert.Contains("já foi processado", exception.Message);
        Assert.Equal(0m, conta.Balance);

        _repositoryMock.Verify(
            repository => repository.GetAccountForUpdateAsync(
                conta.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            repository => repository.ExistsByEventIdAsync(
                requisicao.EventId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            repository => repository.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /**
     * Testa que a conta é bloqueada antes da verificação de idempotência.
     */
    [Fact(DisplayName = "Deve bloquear a conta antes de verificar a duplicidade do evento")]
    public async Task Deve_Bloquear_Conta_Antes_De_Verificar_Duplicidade()
    {
        var conta = new Account(Guid.NewGuid());
        var ordemChamadas = new List<string>();

        var requisicao = new ProcessTransactionRequest(
            Guid.NewGuid(),
            conta.Id,
            TransactionType.Credit,
            10m,
            DateTime.UtcNow);

        _repositoryMock
            .Setup(repository => repository.GetAccountForUpdateAsync(
                conta.Id,
                It.IsAny<CancellationToken>()))
            .Callback(() => ordemChamadas.Add("lock"))
            .ReturnsAsync(conta);

        _repositoryMock
            .Setup(repository => repository.ExistsByEventIdAsync(
                requisicao.EventId,
                It.IsAny<CancellationToken>()))
            .Callback(() => ordemChamadas.Add("exists"))
            .ReturnsAsync(false);

        await _service.ProcessAsync(requisicao);

        Assert.Equal(new[] { "lock", "exists" }, ordemChamadas);
    }

    /**
     * Testa o processamento de uma transação para uma conta inexistente.
     * Deve lançar uma exceção KeyNotFoundException e não gravar nada.
     */
    [Fact(DisplayName = "Deve lançar exceção quando a conta não existir")]
    public async Task Deve_Lancar_Excecao_Quando_Conta_Nao_Existir()
    {
        var requisicao = new ProcessTransactionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                TransactionType.Credit,
                100m,
                DateTime.UtcNow);

        _repositoryMock
            .Setup(repository => repository.GetAccountForUpdateAsync(
                requisicao.AccountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ProcessAsync(requisicao));

        Assert.Contains("não foi encontrada", excecao.Message);

        _repositoryMock.Verify(
            repository => repository.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /**
     * Testa o processamento de uma transação de crédito válida.
     * Deve aumentar o saldo da conta e registrar a transação.
     */
    [Fact(DisplayName = "Deve aumentar o saldo e registrar a transação ao processar um crédito válido")]
    public async Task Deve_Aumentar_Saldo_E_Registrar_Transacao_Ao_Processar_Credito_Valido()
    {
        var conta = new Account(Guid.NewGuid());

        var requisicao = new ProcessTransactionRequest(
            Guid.NewGuid(),
            conta.Id,
            TransactionType.Credit,
            150.75m,
            DateTime.UtcNow);

        _repositoryMock
            .Setup(repository => repository.ExistsByEventIdAsync(
                requisicao.EventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(repository => repository.GetAccountForUpdateAsync(
                requisicao.AccountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        await _service.ProcessAsync(requisicao);

        Assert.Equal(150.75m, conta.Balance);

        _repositoryMock.Verify(
            repository => repository.AddTransactionAsync(
                It.Is<Transaction>(transacao =>
                    transacao.AccountId == conta.Id &&
                    transacao.Amount == requisicao.Amount &&
                    transacao.Type == requisicao.Type &&
                    transacao.EventId == requisicao.EventId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /**
     * Testa o processamento de uma transação de débito com saldo insuficiente.
     * Deve lançar uma exceção InvalidOperationException.
     */
    [Fact(DisplayName = "Deve rejeitar débito quando o saldo for insuficiente")]
    public async Task Deve_Rejeitar_Debito_Quando_Saldo_For_Insuficiente()
    {
        var conta = new Account(Guid.NewGuid());
        conta.Credit(50m);

        var requisicao = new ProcessTransactionRequest(
                Guid.NewGuid(),
                conta.Id,
                TransactionType.Debit,
                100m,
                DateTime.UtcNow);

        _repositoryMock
            .Setup(repository => repository.ExistsByEventIdAsync(
                requisicao.EventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(repository => repository.GetAccountForUpdateAsync(
                requisicao.AccountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>
        (() => _service.ProcessAsync(requisicao));

        Assert.Contains("Insufficient balance", exception.Message);
        Assert.Equal(50m, conta.Balance);

        _repositoryMock.Verify(
            repository => repository.AddTransactionAsync(
                It.IsAny<Transaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}