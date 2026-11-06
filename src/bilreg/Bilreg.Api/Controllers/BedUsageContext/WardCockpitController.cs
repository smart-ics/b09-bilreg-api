using Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
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
    private readonly IBedRepo? _bedRepo;

    public WardCockpitController(
        IMediator mediator,
        IWardCockpitEventPublisher? eventPublisher = null,
        IBedRepo? bedRepo = null)
    {
        _mediator = mediator;
        _eventPublisher = eventPublisher ?? new NullWardCockpitEventPublisher();
        _bedRepo = bedRepo;
    }

    [HttpGet("bangsal-list")]
    public async Task<IActionResult> ListBangsal()
    {
        var result = await _mediator.Send(new WardCockpitBangsalListQuery());
        return Ok(new JSendOk(result));
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
        var targetBangsalId = !string.IsNullOrWhiteSpace(bangsalId) ? bangsalId : result.BangsalId;
        if (string.IsNullOrWhiteSpace(targetBangsalId) && !string.IsNullOrWhiteSpace(cmd.LayananId))
        {
            targetBangsalId = cmd.LayananId;
        }
        if (string.IsNullOrWhiteSpace(targetBangsalId) && _bedRepo != null && !string.IsNullOrWhiteSpace(cmd.BedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(cmd.BedId));
            if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
            {
                targetBangsalId = maybeBed.Value.Bangsal.BangsalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(targetBangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(targetBangsalId, cmd.BedId, "Occupied", result);
            await _eventPublisher.PublishWaitingListUpdatedAsync(targetBangsalId);
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("transfer")]
    [HttpPost("/api/ward-cockpit/transfer")]
    public async Task<IActionResult> Transfer([FromBody] PakaiBedTransferCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        var srcBangsalId = !string.IsNullOrWhiteSpace(result.SourceBangsalId) ? result.SourceBangsalId : bangsalId;
        var tgtBangsalId = !string.IsNullOrWhiteSpace(result.TargetBangsalId) ? result.TargetBangsalId : bangsalId;

        if (string.IsNullOrWhiteSpace(srcBangsalId) && _bedRepo != null && !string.IsNullOrWhiteSpace(result.SourceBedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(result.SourceBedId));
            if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
            {
                srcBangsalId = maybeBed.Value.Bangsal.BangsalId;
            }
        }
        if (string.IsNullOrWhiteSpace(tgtBangsalId) && _bedRepo != null && !string.IsNullOrWhiteSpace(result.TargetBedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(result.TargetBedId));
            if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
            {
                tgtBangsalId = maybeBed.Value.Bangsal.BangsalId;
            }
        }

        // 1. Broadcast Cleaning status to old (source) bed so it immediately enters HK mode
        if (!string.IsNullOrWhiteSpace(srcBangsalId) && !string.IsNullOrWhiteSpace(result.SourceBedId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(srcBangsalId, result.SourceBedId, "Cleaning");
        }

        // 2. Broadcast Occupied status to target bed
        if (!string.IsNullOrWhiteSpace(tgtBangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(tgtBangsalId, result.TargetBedId, "Occupied", result);
        }

        // 3. Broadcast WaitingListUpdated to both source and target wards
        if (!string.IsNullOrWhiteSpace(srcBangsalId))
        {
            await _eventPublisher.PublishWaitingListUpdatedAsync(srcBangsalId);
        }

        if (!string.IsNullOrWhiteSpace(tgtBangsalId) &&
            !string.Equals(tgtBangsalId.Trim(), srcBangsalId?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await _eventPublisher.PublishWaitingListUpdatedAsync(tgtBangsalId);
        }

        return Ok(new JSendOk(result));
    }

    [HttpPost("discharge")]
    public async Task<IActionResult> Discharge([FromBody] PakaiBedReleaseCommand cmd, [FromQuery] string? bangsalId = null, [FromQuery] string? bedId = null)
    {
        var result = await _mediator.Send(cmd);
        var targetBangsalId = !string.IsNullOrWhiteSpace(bangsalId) ? bangsalId : result.BangsalId;
        var targetBedId = (!string.IsNullOrWhiteSpace(bedId) ? bedId : (result.BedId ?? string.Empty)).Trim();

        if (string.IsNullOrWhiteSpace(targetBangsalId) && _bedRepo != null && !string.IsNullOrWhiteSpace(targetBedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(targetBedId));
            if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
            {
                targetBangsalId = maybeBed.Value.Bangsal.BangsalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(targetBangsalId) && !string.IsNullOrWhiteSpace(targetBedId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(targetBangsalId, targetBedId, "Cleaning");
        }

        if (result.IsBatalCheckIn)
        {
            if (!string.IsNullOrWhiteSpace(targetBangsalId))
            {
                await _eventPublisher.PublishWaitingListUpdatedAsync(targetBangsalId);
            }

            if (!string.IsNullOrWhiteSpace(result.RevertedWaitingListBangsalId) &&
                !string.Equals(result.RevertedWaitingListBangsalId.Trim(), targetBangsalId?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                await _eventPublisher.PublishWaitingListUpdatedAsync(result.RevertedWaitingListBangsalId);
            }
        }

        return Ok(new JSendOk(result));
    }

    [HttpGet("lookup-baby")]
    public async Task<IActionResult> LookupBaby([FromQuery] string keyword)
    {
        var result = await _mediator.Send(new WardCockpitLookupBabyQuery(keyword));
        return Ok(new JSendOk(result));
    }

    [HttpPost("rooming-in")]
    public async Task<IActionResult> RoomingIn([FromBody] WardRoomingInCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        if (!string.IsNullOrWhiteSpace(bangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(bangsalId, cmd.BedId, "Occupied", result);
            await _eventPublisher.PublishWaitingListUpdatedAsync(bangsalId);
        }

        if (!string.IsNullOrWhiteSpace(result.BabyWaitingListBangsalId) &&
            !string.Equals(result.BabyWaitingListBangsalId.Trim(), bangsalId?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await _eventPublisher.PublishWaitingListUpdatedAsync(result.BabyWaitingListBangsalId);
        }

        return Ok(new JSendOk(result));
    }

    [HttpPost("hk/complete")]
    public async Task<IActionResult> CompleteHk([FromBody] BedOperationalVerifyReadyCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        var targetBangsalId = !string.IsNullOrWhiteSpace(bangsalId) ? bangsalId : cmd.LayananId;
        if (string.IsNullOrWhiteSpace(targetBangsalId) && _bedRepo != null && !string.IsNullOrWhiteSpace(cmd.BedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(cmd.BedId));
            if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
            {
                targetBangsalId = maybeBed.Value.Bangsal.BangsalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(targetBangsalId))
        {
            await _eventPublisher.PublishBedStatusChangedAsync(targetBangsalId, cmd.BedId, "Ready");
            await _eventPublisher.PublishWaitingListUpdatedAsync(targetBangsalId);
        }
        return Ok(new JSendOk(result));
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> Maintenance([FromBody] BedOperationalSetBlockerCommand cmd, [FromQuery] string? bangsalId = null)
    {
        var result = await _mediator.Send(cmd);
        var targetBangsalId = !string.IsNullOrWhiteSpace(bangsalId) ? bangsalId : null;
        if (string.IsNullOrWhiteSpace(targetBangsalId) && _bedRepo != null && !string.IsNullOrWhiteSpace(cmd.BedId))
        {
            var maybeBed = _bedRepo.LoadEntity(BedType.Key(cmd.BedId));
            if (maybeBed.HasValue && !string.IsNullOrWhiteSpace(maybeBed.Value.Bangsal?.BangsalId) && maybeBed.Value.Bangsal.BangsalId != "-")
            {
                targetBangsalId = maybeBed.Value.Bangsal.BangsalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(targetBangsalId))
        {
            var status = cmd.IsBlocked ? "Maintenance" : "Ready";
            await _eventPublisher.PublishBedStatusChangedAsync(targetBangsalId, cmd.BedId, status);
            await _eventPublisher.PublishWaitingListUpdatedAsync(targetBangsalId);
        }
        return Ok(new JSendOk(result));
    }
}
