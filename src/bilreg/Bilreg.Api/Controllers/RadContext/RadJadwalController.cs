using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.RadContext;

[Route("api/rad/jadwal")]
[ApiController]
[Authorize]
public class RadJadwalController : ControllerBase
{
    private readonly IMediator _mediator;

    public RadJadwalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RadJadwalCreateCmd cmd, CancellationToken ct)
    {
        try
        {
            var response = await _mediator.Send(cmd, ct);
            return Ok(new JSendOk(response));
        }
        catch (RadJadwalSlotConflictException ex)
        {
            return Conflict(new JSendFail(new
            {
                code = RadJadwalSlotConflictException.Code,
                message = ex.Message
            }));
        }
    }

    [HttpPost("{id}/reschedule")]
    public async Task<IActionResult> Reschedule(
        string id,
        [FromBody] RescheduleRadJadwalRequest req,
        CancellationToken ct)
    {
        try
        {
            var cmd = new RadJadwalRescheduleCmd(
                id,
                req.NewEquipmentId,
                req.NewScheduleDate,
                req.NewStartTime,
                req.NewEndTime,
                req.Reason,
                req.UserId);
            var response = await _mediator.Send(cmd, ct);
            return Ok(new JSendOk(response));
        }
        catch (RadJadwalSlotConflictException ex)
        {
            return Conflict(new JSendFail(new
            {
                code = RadJadwalSlotConflictException.Code,
                message = ex.Message
            }));
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(
        string id,
        [FromBody] CancelRadJadwalRequest req,
        CancellationToken ct)
    {
        var response = await _mediator.Send(
            new RadJadwalCancelCmd(id, req.Category, req.Reason, req.UserId),
            ct);
        return Ok(new JSendOk(response));
    }

    [HttpPost("{id}/clearance")]
    public async Task<IActionResult> Clearance(
        string id,
        [FromBody] ClearanceRadJadwalRequest req,
        CancellationToken ct)
    {
        var response = await _mediator.Send(
            new RadJadwalClearanceCmd(
                id,
                req.IsArrived,
                req.IsCleared,
                req.ClearanceNotes,
                req.UserId,
                req.SafetyChecklist),
            ct);
        return Ok(new JSendOk(response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var response = await _mediator.Send(new RadJadwalGetQuery(id), ct);
        if (response is null)
            return NotFound(new JSendFail($"Jadwal radiologi '{id}' tidak ditemukan."));

        return Ok(new JSendOk(response));
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateTime? scheduleDate = null,
        [FromQuery] string? modality = null,
        [FromQuery] string? equipmentId = null,
        [FromQuery] DateTime? date = null,
        CancellationToken ct = default)
    {
        var targetDate = scheduleDate ?? date ?? DateTime.Today;
        var response = await _mediator.Send(
            new RadJadwalCalendarTimelineQuery(targetDate, modality, equipmentId),
            ct);
        return Ok(new JSendOk(response));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? modality = null,
        [FromQuery] int? status = null,
        [FromQuery] string? equipmentId = null,
        CancellationToken ct = default)
    {
        var effectiveStart = startDate ?? DateTime.Today;
        var effectiveEnd = endDate ?? effectiveStart;
        var response = await _mediator.Send(
            new RadJadwalWorklistQuery(effectiveStart, effectiveEnd, modality, status, equipmentId),
            ct);
        return Ok(new JSendOk(response));
    }
}

public record RescheduleRadJadwalRequest(
    string NewEquipmentId,
    DateOnly NewScheduleDate,
    TimeOnly NewStartTime,
    TimeOnly NewEndTime,
    string Reason,
    string UserId);

public record CancelRadJadwalRequest(
    RadCancellationCategoryEnum Category,
    string Reason,
    string UserId);

public record ClearanceRadJadwalRequest(
    bool IsArrived,
    bool IsCleared,
    string ClearanceNotes,
    string UserId,
    RadSafetyChecklistType? SafetyChecklist = null);
