using Fraga.Application.Transactions;
using Fraga.Application.Transactions.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Fraga.Api.Controllers;

/**
 * Controlador responsável por receber e processar eventos financeiros.
 */
[ApiController]
[Route("api/transactions")]
public class TransactionsController(
    ITransactionService transactionService) : ControllerBase
{
    /// <summary>
    /// Processa um evento financeiro de crédito ou débito.
    /// </summary>
    /// <remarks>
    /// O processamento é idempotente por <c>eventId</c>: reenviar o mesmo evento
    /// retorna 409. Débitos acima do saldo retornam 422.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ProcessTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ProcessAsync(
        [FromBody] ProcessTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await transactionService.ProcessAsync(
            request,
            cancellationToken);

        return Ok(response);
    }
}