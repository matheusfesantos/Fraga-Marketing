using Fraga.Application.Accounts;
using Fraga.Application.Accounts.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Fraga.Api.Controllers;

/**
 * Controlador responsável por gerenciar as operações relacionadas
 * às contas.
 */
[ApiController]
[Route("api/accounts")]
public class AccountsController(AccountService service) : ControllerBase
{
    private const string GetAccountByIdRoute = "GetAccountById";

    /// <summary>
    /// Cria uma nova conta com saldo zero.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await service.CreateAsync(request, cancellationToken);

        return CreatedAtRoute(
            GetAccountByIdRoute,
            new { accountId = account.Id },
            account);
    }

    /// <summary>
    /// Lista todas as contas com seus saldos atuais.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await service.GetAllAsync(cancellationToken);

        return Ok(accounts);
    }

    /// <summary>
    /// Retorna uma conta pelo seu identificador.
    /// </summary>
    [HttpGet("{accountId:guid}", Name = GetAccountByIdRoute)]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await service.GetByIdAsync(
            accountId,
            cancellationToken);

        if (account is null)
            return NotFound();

        return Ok(account);
    }

    /// <summary>
    /// Retorna o extrato paginado de uma conta, da transação mais recente para a mais antiga.
    /// </summary>
    [HttpGet("{accountId:guid}/transactions")]
    [ProducesResponseType(
        typeof(PagedResponse<TransactionStatementItemResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatementAsync(
        Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var statement = await service.GetStatementAsync(
            accountId,
            page,
            pageSize,
            cancellationToken);

        return Ok(statement);
    }
}