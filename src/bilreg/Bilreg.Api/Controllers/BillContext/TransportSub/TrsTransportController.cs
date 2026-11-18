using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TransportSub;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TrsTransportController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrsTransportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [HttpPost("request")]
    public async Task<IActionResult> RequestTransport([FromBody] RequestTransportCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("dispatch")]
    [HttpPost("{id}/dispatch")]
    public async Task<IActionResult> Dispatch([FromBody] DispatchTransportCmd cmd, string? id = null)
    {
        var finalCmd = !string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(cmd.TrsTransportId)
            ? cmd with { TrsTransportId = id }
            : cmd;
        var result = await _mediator.Send(finalCmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("link-reg")]
    [HttpPost("{id}/link-reg")]
    public async Task<IActionResult> LinkReg([FromBody] LinkRegTransportCmd cmd, string? id = null)
    {
        var finalCmd = !string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(cmd.TrsTransportId)
            ? cmd with { TrsTransportId = id }
            : cmd;
        var result = await _mediator.Send(finalCmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("complete")]
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteTransportCmd cmd, string? id = null)
    {
        var finalCmd = !string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(cmd.TrsTransportId)
            ? cmd with { TrsTransportId = id }
            : cmd;
        var result = await _mediator.Send(finalCmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("cancel")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelTransportCmd cmd, string? id = null)
    {
        var finalCmd = !string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(cmd.TrsTransportId)
            ? cmd with { TrsTransportId = id }
            : cmd;
        var result = await _mediator.Send(finalCmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new GetTransportQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("by-reg/{regId}")]
    [HttpGet("list/{regId}")]
    public async Task<IActionResult> ListByReg(string regId)
    {
        var query = new ListTransportByRegQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("active")]
    [HttpGet("active-dispatch")]
    public async Task<IActionResult> ListActiveDispatch()
    {
        var query = new ListActiveDispatchQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}
