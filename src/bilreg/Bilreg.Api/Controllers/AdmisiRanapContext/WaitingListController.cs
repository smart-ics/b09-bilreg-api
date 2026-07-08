using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/waiting-list")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
public class WaitingListController : ControllerBase
{
    private readonly IMediator _mediator;

    public WaitingListController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdmCreateWaitingListCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdmUpdateWaitingListBody body)
    {
        var cmd = new AdmUpdateWaitingListCmd(
            id,
            body.Priority,
            body.KelasId,
            body.BangsalId,
            body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(string id, [FromBody] AdmCloseWaitingListBody body)
    {
        await _mediator.Send(new AdmCloseWaitingListCmd(id, body.UserId));
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new AdmGetWaitingListQry(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? bangsalId,
        [FromQuery] int? waitingListStatus)
    {
        var result = await _mediator.Send(new AdmListWaitingListQry(bangsalId, waitingListStatus));
        return Ok(new JSendOk(result));
    }
}

public record AdmUpdateWaitingListBody(
    int Priority,
    string KelasId,
    string BangsalId,
    string UserId);

public record AdmCloseWaitingListBody(string UserId);
