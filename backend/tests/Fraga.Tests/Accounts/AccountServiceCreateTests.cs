using Fraga.Application.Abstractions;
using Fraga.Application.Accounts;
using Fraga.Application.Accounts.DTOs;
using Fraga.Domain.Entities;
using Moq;

namespace Fraga.Tests.Accounts;

/**
 * Testes unitários para a criação de contas no AccountService.
 */
public sealed class AccountServiceCreateTests
{
    private readonly Mock<IAccountRepository> _repositoryMock = new();
    private readonly Mock<ILogService> _logServiceMock = new();
    private readonly AccountService _service;

    public AccountServiceCreateTests()
    {
        _service = new AccountService(
            _repositoryMock.Object,
            _logServiceMock.Object);
    }

    [Fact(DisplayName = "Deve criar a conta com saldo zero e persisti-la")]
    public async Task CriarAsync_NomeValido_PersisteContaComSaldoZero()
    {
        Account? persistida = null;

        _repositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Account>(),
                It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((conta, _) => persistida = conta)
            .Returns(Task.CompletedTask);

        var resposta = await _service.CreateAsync(
            new CreateAccountRequest("  Conta Salário  "));

        Assert.NotNull(persistida);
        Assert.Equal("Conta Salário", resposta.name);
        Assert.Equal(0m, resposta.Balance);
        Assert.NotEqual(Guid.Empty, resposta.Id);
        Assert.Equal(persistida.Id, resposta.Id);

        _repositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Account>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory(DisplayName = "Deve rejeitar a criação com nome inválido sem persistir")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CriarAsync_NomeInvalido_LancaExcecaoENaoPersiste(string nome)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(new CreateAccountRequest(nome)));

        _repositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Account>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}