using Fraga.Application.Abstractions;
using Fraga.Application.Accounts;
using Fraga.Domain.Entities;
using Fraga.Domain.Exceptions;
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

    public sealed class AccountTests
    {
        [Fact(DisplayName = "Deve criar a conta com saldo zero")]
        public void NovaConta_ComecaComSaldoZero()
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");

            Assert.Equal(0m, conta.Balance);
        }

        [Fact(DisplayName = "Deve aumentar o saldo ao creditar")]
        public void Credit_ValorValido_AumentaSaldo()
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");

            conta.Credit(100.50m);

            Assert.Equal(100.50m, conta.Balance);
        }

        [Theory(DisplayName = "Deve rejeitar crédito com valor não positivo")]
        [InlineData(0)]
        [InlineData(-10)]
        public void Credit_ValorNaoPositivo_LancaExcecao(decimal valor)
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");

            Assert.Throws<ArgumentOutOfRangeException>(() => conta.Credit(valor));
            Assert.Equal(0m, conta.Balance);
        }

        [Fact(DisplayName = "Deve reduzir o saldo ao debitar")]
        public void Debit_SaldoSuficiente_ReduzSaldo()
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");
            conta.Credit(100m);

            conta.Debit(40m);

            Assert.Equal(60m, conta.Balance);
        }

        [Fact(DisplayName = "Deve permitir debitar exatamente o saldo disponível")]
        public void Debit_ValorIgualAoSaldo_ZeraSaldo()
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");
            conta.Credit(100m);

            conta.Debit(100m);

            Assert.Equal(0m, conta.Balance);
        }

        [Fact(DisplayName = "Deve rejeitar débito acima do saldo sem alterá-lo")]
        public void Debit_SaldoInsuficiente_LancaExcecaoESaldoIntacto()
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");
            conta.Credit(50m);

            Assert.Throws<InsufficientBalanceException>(() => conta.Debit(50.01m));
            Assert.Equal(50m, conta.Balance);
        }

        [Theory(DisplayName = "Deve rejeitar débito com valor não positivo")]
        [InlineData(0)]
        [InlineData(-10)]
        public void Debit_ValorNaoPositivo_LancaExcecaoESaldoIntacto(decimal valor)
        {
            var conta = new Account(Guid.NewGuid(), "Conta de teste");
            conta.Credit(100m);

            Assert.Throws<ArgumentOutOfRangeException>(() => conta.Debit(valor));
            Assert.Equal(100m, conta.Balance);
        }

        [Fact(DisplayName = "Deve criar a conta com o nome informado sem espaços nas pontas")]
        public void NovaConta_NomeComEspacos_GuardaNomeAparado()
        {
            var conta = new Account(Guid.NewGuid(), "  Conta Corrente  ");

            Assert.Equal("Conta Corrente", conta.Name);
        }

        [Theory(DisplayName = "Deve rejeitar nome vazio ou em branco")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void NovaConta_NomeVazio_LancaExcecao(string? nome)
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Account(Guid.NewGuid(), nome!));

            Assert.Contains("nome da conta", exception.Message);
        }

        [Fact(DisplayName = "Deve rejeitar nome acima do tamanho máximo")]
        public void NovaConta_NomeMuitoLongo_LancaExcecao()
        {
            var nome = new string('a', Account.NameMaxLength + 1);

            Assert.Throws<ArgumentException>(
                () => new Account(Guid.NewGuid(), nome));
        }

        [Fact(DisplayName = "Deve rejeitar identificador vazio")]
        public void NovaConta_IdentificadorVazio_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(
                () => new Account(Guid.Empty, "Conta Corrente"));
        }
    }
}