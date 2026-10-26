using System.Security.Claims;
using Bilreg.Application.PaymentContext.DepositFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.DepositSub;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DepositController : Controller
{
    private readonly IMediator _mediator;
    public DepositController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> Save(DepositCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new DepositGetQry(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("list/{regId}")]
    public async Task<IActionResult> ListByReg(string regId)
    {
        var query = new DepositListQry(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("saldo/{regId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSaldo(string regId)
    {
        var query = new DepositSaldoGetQry(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpPut]
    [Route("{id}/void")]
    public async Task<IActionResult> Void(string id)
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value
            ?? User?.FindFirst(ClaimTypes.Name)?.Value
            ?? User?.Identity?.Name
            ?? string.Empty;

        var cmd = new DepositVoidCmd(id, userId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Deposit berhasil dibatalkan."));
    }
}
