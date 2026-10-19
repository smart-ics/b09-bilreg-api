using System.Security.Claims;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext.RoomChargeFeature;

[Route("api/charge/room-charge")]
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RoomChargeController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomChargeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("process-daily-cutoff")]
    public async Task<IActionResult> ProcessDailyCutOff([FromBody] RoomChargePostDailyCutOffRequest? req = null)
    {
        var userId = GetActorUserId(req?.UserId);
        var cmd = new RoomChargePostDailyBatchCmd(req?.BangsalId, req?.CutOffDate, userId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(new
        {
            result.TotalProcessed,
            result.TotalSkipped,
            result.TotalFailed
        }));
    }

    [HttpPost("reconcile-checkout")]
    public async Task<IActionResult> ReconcileCheckout([FromBody] RoomChargeReconcileCheckoutRequest req)
    {
        var userId = GetActorUserId(req.UserId);
        var cmd = new RoomChargeReconcileCheckoutCmd(req.PakaiBedId, req.CheckOutTime, userId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/reverse")]
    public async Task<IActionResult> Reverse(string id, [FromBody] RoomChargeReverseRequest req)
    {
        var userId = GetActorUserId(req.UserId);
        var cmd = new RoomChargeReverseCmd(id, userId, req.Reason);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var query = new RoomChargeGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("reg/{regId}")]
    [HttpGet("{regId}/register")]
    public async Task<IActionResult> ListByReg(string regId)
    {
        var query = new RoomChargeListByRegQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("reg/{regId}/summary")]
    public async Task<IActionResult> SummaryByReg(string regId)
    {
        var query = new RoomChargeSummaryByRegQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    private string GetActorUserId(string? fallbackUserId = null)
    {
        if (!string.IsNullOrWhiteSpace(fallbackUserId))
            return fallbackUserId;

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub")
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? User?.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(userId))
            return userId;

        if (Request?.Headers.TryGetValue("X-User-Id", out var headerVal) == true && !string.IsNullOrWhiteSpace(headerVal))
            return headerVal.ToString();

        return "SYSTEM";
    }
}

public record RoomChargePostDailyCutOffRequest(string? BangsalId = null, DateTime? CutOffDate = null, string? UserId = null);
public record RoomChargeReconcileCheckoutRequest(string PakaiBedId, DateTime CheckOutTime, string? UserId = null);
public record RoomChargeReverseRequest(string Reason, string? UserId = null);
