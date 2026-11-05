using Bilreg.Api.Controllers.BedUsageContext.WaitingListFeature;
using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using BedUsageUseCases = Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

/// <summary>
/// Backward-compatible adapter for WaitingList operations under the AdmisiRanapContext route.
/// Callers are encouraged to use /api/bed-usage/waiting-list (BedUsageContext.WaitingListFeature).
/// </summary>
[Route("api/admisi-ranap/waiting-list")]
//[ApiController]
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

    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> Reserve(string id, [FromBody] WaitingListReserveBody body)
    {
        var cmd = new BedUsageUseCases.WaitingListReserveCmd(id, body.BedId, body.UserId, body.BedName);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] WaitingListCancelBody body)
    {
        var cmd = new BedUsageUseCases.WaitingListCancelCmd(id, body.Reason, body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/reroute")]
    public async Task<IActionResult> Reroute(string id, [FromBody] WaitingListRerouteBody body)
    {
        var cmd = new BedUsageUseCases.WaitingListRerouteCmd(
            id,
            body.TargetBangsalId,
            body.Reason,
            body.UserId,
            body.TargetKelasId,
            body.ClinicalUrgency,
            body.SpecialRequirements,
            body.PlannedBedId,
            body.PlannedBedName);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("by-reg/{regId}")]
    public async Task<IActionResult> GetByReg(string regId)
    {
        var result = await _mediator.Send(new AdmGetWaitingListByRegIdQry(regId));
        return Ok(new JSendOk(result));
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

    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklist([FromQuery] string bangsalId)
    {
        var result = await _mediator.Send(new BedUsageUseCases.WaitingListWorklistQuery(bangsalId));
        return Ok(new JSendOk(result));
    }
}

public record AdmUpdateWaitingListBody(
    int Priority,
    string KelasId,
    string BangsalId,
    string UserId);

public record AdmCloseWaitingListBody(string UserId);
