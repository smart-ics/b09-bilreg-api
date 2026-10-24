using Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext;

[Route("api/v1/ward-cockpit")]
[ApiController]
[Authorize]
public class WardCockpitController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWardCockpitEventPublisher _eventPublisher;

    public WardCockpitController(IMediator mediator, IWardCockpitEventPublisher? eventPublisher = null)
    {
        _mediator = mediator;
        _eventPublisher = eventPublisher ?? new NullWardCockpitEventPublisher();
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] string bangsalId)
    {
        var result = await _mediator.Send(new WardCockpitOverviewQuery(bangsalId));
        return Ok(new JSendOk(result));
    }

    [HttpGet("bed-detail/{bedId}")]
    public async Task<IActionResult> GetBedDetail(string bedId)
    {
        var result = await _mediator.Send(new WardCockpitBedDetailQuery(bedId));
        return Ok(new JSendOk(result));
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] WardCheckInCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, cmd.BedId, "Occupied", result);
            await _eventPublisher.PublishWaitingListUpdatedAsync(bangsalId);
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] PakaiBedTransferCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, cmd.TargetBedId, "Occupied", result);
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("discharge")]
    public async Task<IActionResult> Discharge([FromBody] PakaiBedReleaseCommand cmd, [FromQuery] string? bangsalId = null, [FromQuery] string? bedId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId) && !string.IsNullOrWhiteSpace(bedId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, bedId, "CleaningRequired");
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("rooming-in")]
    public async Task<IActionResult> RoomingIn([FromBody] WardRoomingInCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, cmd.BedId, "Occupied", result);
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("hk/complete")]
    public async Task<IActionResult> CompleteHk([FromBody] BedOperationalVerifyReadyCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, cmd.BedId, "Ready");
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance([FromBody] BedOperationalSetBlockerCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId))
        {
            var status = cmd.IsBlocked ? cmd.RestrictionType.ToString() : "Ready";
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, cmd.BedId, status);
        }
        return Ok(new JSendOk(result));
    }
}
