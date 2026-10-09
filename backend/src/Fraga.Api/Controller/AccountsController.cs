using Fraga.Application.Accounts;
using Fraga.Application.Accounts.DTOs;
using Fraga.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Fraga.Api.Controllers;

/**
 * Controlador responsável por gerenciar as operações relacionadas
 * às contas.
 */
[ApiController]
[Route("api/[controller]")]
public class AccountsController(AccountService service) : ControllerBase
{
    /**
     * Retorna todas as contas.
     */
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Account>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await service.GetAllAsync(
            cancellationToken);
        return Ok(accounts);
    }

    /**
     * Retorna uma conta pelo seu ID.
     */
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(typeof(Account), StatusCodes.Status200OK)]
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

    /**
     * Retorna o extrato de transações de uma conta.
     */
    [HttpGet("{accountId:guid}/transactions")]
    [ProducesResponseType(typeof(
        PagedResponse<TransactionStatementItemResponse>), 
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            if (statement is null)
                return NotFound();

            return Ok(statement);
    }
}