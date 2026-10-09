using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

/**
 * Implementação de um manipulador global de exceções.
 * Este manipulador captura exceções não tratadas e retorna
 * respostas HTTP apropriadas para o cliente.
 */
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    /**
     * Tenta manipular a exceção fornecida e retorna uma resposta HTTP
     * apropriada para o cliente.
     *
     * @param httpContext O contexto HTTP da solicitação atual.
     * @param exception A exceção que ocorreu durante o processamento da solicitação.
     * @param cancellationToken Um token de cancelamento para operações assíncronas.
     * @returns Um valor booleano indicando se a exceção foi manipulada com sucesso.
     */
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var(statusCode, message) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest, "o parâmetro fornecido é inválido."),
            
            KeyNotFoundException => (
                StatusCodes.Status404NotFound, "O recurso solicitado não foi encontrado."),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
        };

        if (statusCode == 500)
        {
            logger.LogError(
                exception, 
                "Erro não tratado ocorreu ao processar a solicitação.");
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
            Type = "https://httpstatuses.com/" + statusCode,
            Instance = httpContext.Request.Path
        };

        if(500 < statusCode)
        {
            problemDetails.Detail = exception.Message;
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails, 
            cancellationToken: cancellationToken);

        return true;
    }
}