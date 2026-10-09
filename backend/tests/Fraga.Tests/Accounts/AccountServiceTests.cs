using Fraga.Application.Abstractions;
using Fraga.Application.Accounts;
using Fraga.Domain.Entities;
using Moq;

namespace Fraga.Tests.Accounts;

/**
 * Testes unitários para a classe AccountService.
 */
public sealed class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _repositoryMock = new();
    private readonly Mock<ILogService> _logServiceMock = new();
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _service = new AccountService(
            _repositoryMock.Object,
            _logServiceMock.Object);
    }

    /**
     * Testa o método GetAllAsync quando não há contas cadastradas.
     * Deve retornar uma lista vazia.
     */
    [Fact(DisplayName = "Deve retornar lista vazia quando não houver contas cadastradas")]
    public async Task ListarAsync_QuandoNaoExistemContas_RetornaListaVazia()
    {
        _repositoryMock
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var servico = _service;

        var resultado = await servico.GetAllAsync();

        Assert.NotNull(resultado);
        Assert.Empty(resultado);

        _repositoryMock.Verify(
            repository => repository.GetAllAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /**
     * Testa o método GetByIdAsync quando o identificador da conta é vazio.
     * Deve lançar uma exceção ArgumentException.
     */
    [Fact(DisplayName = "Deve lançar exceção quando o identificador da conta for vazio")]
    public async Task ObterPorIdAsync_QuandoIdentificadorVazio_LancaExcecao()
    {
        var servico = _service;

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => servico.GetByIdAsync(Guid.Empty));

        Assert.Equal("accountId", exception.ParamName);

        _repositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /**
     * Testa o método GetByIdAsync quando a conta não existe.
     * Deve retornar nulo.
     */
    [Fact(DisplayName = "Deve retornar nulo quando a conta não existir")]
    public async Task ObterPorIdAsync_QuandoContaNaoExiste_RetornaNulo()
    {
        var indentificadorConta = Guid.NewGuid();

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(
                indentificadorConta,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var servico = _service;

        var resultado = await servico.GetByIdAsync(indentificadorConta);

        Assert.Null(resultado);

        _repositoryMock.Verify(
            repository => repository.GetByIdAsync(
                indentificadorConta,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /**
     * Testa o método GetStatementAsync quando a página é menor que 1.
     * Deve lançar uma exceção ArgumentOutOfRangeException.
     */
    [Fact(DisplayName = "Deve lançar exceção quando a página for menor que 1")]
    public async Task ObterExtratoAsync_QuandoPaginaMenorQueUm_LancaExcecao()
    {
        var servico = _service;

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servico.GetStatementAsync(
                Guid.NewGuid(),
                page: 0,
                pageSize: 10));

        Assert.Equal("page", exception.ParamName);
        
        _repositoryMock.Verify(
            repository => repository.GetStatementAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /**
     * Testa o método GetStatementAsync quando o tamanho da página é inválido.
     * Deve lançar uma exceção ArgumentOutOfRangeException.
     */
    [Theory(DisplayName = "Deve lançar exceção quando o tamanho da página for inválido")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task ObterExtratoAsync_QuandoTamanhoPaginaInvalido_LancaExcecao(
        int tamanhoPagina)
    {
        var servico = _service;

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servico.GetStatementAsync(
                Guid.NewGuid(),
                page: 1,
                pageSize: tamanhoPagina));

        Assert.Equal("pageSize", exception.ParamName);
        
        _repositoryMock.Verify(
            repository => repository.GetStatementAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Deve lançar exceção quando a conta não existir")]
    public async Task ObterExtratoAsync_QuandoContaNaoExiste_LancaExcecao()
    {
        var identificadorConta = Guid.NewGuid();

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(
                identificadorConta,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var servico = _service;

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => servico.GetStatementAsync(
                identificadorConta,
                page: 1,
                pageSize: 10));

        Assert.Equal("Conta não encontrada.", exception.Message);

        _repositoryMock.Verify(
            repository => repository.GetStatementAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}