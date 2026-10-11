using System.Text.Json;
using Fraga.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fraga.Tests.ExceptionHandling;

/**
 * Testes unitários para a classe GlobalExceptionHandler.
 */
public sealed class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int> Cenarios => new()
    {
        { new DuplicateEventException(), StatusCodes.Status409Conflict },
        { new InsufficientBalanceException(), StatusCodes.Status422UnprocessableEntity },
        { new KeyNotFoundException("Conta não encontrada."), StatusCodes.Status404NotFound },
        { new ArgumentException("Valor inválido."), StatusCodes.Status400BadRequest },
        { new ArgumentOutOfRangeException("page", "Página inválida."), StatusCodes.Status400BadRequest },
        { new InvalidOperationException("falha inesperada"), StatusCodes.Status500InternalServerError }
    };

    /**
     * Testa o mapeamento de cada tipo de exceção para o status HTTP esperado.
     */
    [Theory(DisplayName = "Deve mapear a exceção para o status HTTP correto")]
    [MemberData(nameof(Cenarios))]
    public async Task TryHandleAsync_MapeiaExcecaoParaStatusEsperado(
        Exception excecao,
        int statusEsperado)
    {
        var (handler, contexto) = CriarCenario();

        var tratada = await handler.TryHandleAsync(
            contexto,
            excecao,
            CancellationToken.None);

        Assert.True(tratada);
        Assert.Equal(statusEsperado, contexto.Response.StatusCode);
    }

    /**
     * Testa que mensagens de negócio são devolvidas no campo detail.
     */
    [Fact(DisplayName = "Deve expor a mensagem de negócio no detail")]
    public async Task TryHandleAsync_ExcecaoDeNegocio_ExpoeMensagemNoDetail()
    {
        var (handler, contexto) = CriarCenario();

        await handler.TryHandleAsync(
            contexto,
            new DuplicateEventException(),
            CancellationToken.None);

        var corpo = await LerCorpoAsync(contexto);

        Assert.Equal(
            "O evento informado já foi processado.",
            corpo.GetProperty("detail").GetString());
    }

    /**
     * Testa que erros inesperados não vazam detalhes ao cliente.
     */
    [Fact(DisplayName = "Não deve vazar detalhes da exceção em erros 500")]
    public async Task TryHandleAsync_ErroInesperado_NaoVazaDetalhes()
    {
        var (handler, contexto) = CriarCenario();

        await handler.TryHandleAsync(
            contexto,
            new InvalidOperationException("senha=segredo"),
            CancellationToken.None);

        var corpo = await LerCorpoAsync(contexto);

        Assert.False(corpo.TryGetProperty("detail", out var detail)
            && detail.ValueKind == JsonValueKind.String);
        Assert.DoesNotContain("segredo", corpo.ToString());
    }

    private static (GlobalExceptionHandler Handler, DefaultHttpContext Contexto) CriarCenario()
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();

        return (handler, contexto);
    }

    private static async Task<JsonElement> LerCorpoAsync(DefaultHttpContext contexto)
    {
        contexto.Response.Body.Position = 0;

        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);

        return documento.RootElement.Clone();
    }
}