using Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.WaitingListFeature;

[Route("api/bed-usage/waiting-list")]
[ApiController]
[Authorize]
public class WaitingListController : ControllerBase
{
    private readonly IMediator _mediator;

    public WaitingListController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WaitingListCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] WaitingListUpdateBody body)
    {
        var cmd = new WaitingListUpdateCmd(
            id,
            body.KelasId,
            body.BangsalId,
            body.ClinicalUrgency,
            body.SpecialRequirements,
            body.Priority,
            body.UserId,
            body.PlannedBedId,
            body.PlannedBedName);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> Reserve(string id, [FromBody] WaitingListReserveBody body)
    {
        var cmd = new WaitingListReserveCmd(id, body.BedId, body.UserId, body.BedName);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] WaitingListCancelBody body)
    {
        var cmd = new WaitingListCancelCmd(id, body.Reason, body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/reroute")]
    public async Task<IActionResult> Reroute(string id, [FromBody] WaitingListRerouteBody body)
    {
        var cmd = new WaitingListRerouteCmd(
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

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new WaitingListGetQuery(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet("by-reg/{regId}")]
    public async Task<IActionResult> GetByReg(string regId)
    {
        var result = await _mediator.Send(new WaitingListGetByRegQuery(regId));
        return Ok(new JSendOk(result));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklist([FromQuery] string bangsalId)
    {
        var result = await _mediator.Send(new WaitingListWorklistQuery(bangsalId));
        return Ok(new JSendOk(result));
    }
}

public record WaitingListUpdateBody(
    string KelasId,
    string BangsalId,
    ClinicalUrgencyEnum ClinicalUrgency = ClinicalUrgencyEnum.Routine,
    string SpecialRequirements = "",
    int Priority = 0,
    string UserId = "",
    string? PlannedBedId = null,
    string? PlannedBedName = null);

public record WaitingListReserveBody(
    string BedId,
    string UserId,
    string? BedName = null);

public record WaitingListCancelBody(
    string Reason,
    string UserId);

public record WaitingListRerouteBody(
    string TargetBangsalId,
    string Reason,
    string UserId,
    string? TargetKelasId = null,
    ClinicalUrgencyEnum? ClinicalUrgency = null,
    string? SpecialRequirements = null,
    string? PlannedBedId = null,
    string? PlannedBedName = null);
