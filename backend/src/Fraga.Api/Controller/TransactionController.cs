using Fraga.Application.Transactions;
using Fraga.Application.Transactions.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Fraga.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost]
    public async Task<IActionResult> Process(
        [FromBody] ProcessTransactionRequest request,
        CancellationToken cancellationToken)
    {
        await _transactionService.ProcessAsync(request);

        return Ok(new
        {
            message = "Transação processada com sucesso."
        });
    }
}