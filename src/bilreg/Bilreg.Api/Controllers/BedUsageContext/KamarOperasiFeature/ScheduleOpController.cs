using System.Security.Claims;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ScheduleOpController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScheduleOpController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{tgl}/TglOp")]
    public async Task<IActionResult> ListSchedule(string tgl)
    {
        var query = new OkListScheduleQuery(tgl);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("SetSchedule")]
    public async Task<IActionResult> SetSchedule(OkScheduleOpSetCommand cmd)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                cmd = cmd with { UserId = userId };
            }
            var response = await _mediator.Send(cmd);
            return Ok(new JSendOk(response));
        }
        catch (ScheduleCollisionException ex)
        {
            return Conflict(new JSendFail(ex.Message));
        }
        catch (ScheduleWarningException ex)
        {
            return UnprocessableEntity(new JSendFail(new { Warnings = ex.Warnings, RequireOverride = true }));
        }
    }

    [HttpPost("SaveWorkbench")]
    public async Task<IActionResult> SaveWorkbench(OkScheduleOpSaveWorkbenchCommand cmd)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                cmd = cmd with { UserId = userId };
            }
            var response = await _mediator.Send(cmd);
            return Ok(new JSendOk(response));
        }
        catch (ScheduleCollisionException ex)
        {
            return Conflict(new JSendFail(ex.Message));
        }
        catch (ScheduleWarningException ex)
        {
            return UnprocessableEntity(new JSendFail(new { Warnings = ex.Warnings, RequireOverride = true }));
        }
    }

    [HttpPatch]
    [Route("AddPpa")]
    public async Task<IActionResult> AddPpa(OkScheduleOpAddPpaCommand cmd)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                cmd = cmd with { UserId = userId };
            }
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        catch (ScheduleWarningException ex)
        {
            return UnprocessableEntity(new JSendFail(new { Warnings = ex.Warnings, RequireOverride = true }));
        }
    }

    [HttpPatch]
    [Route("RemovePpa")]
    public async Task<IActionResult> RemovePpa(OkScheduleOpRemovePpaCommand cmd)
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            cmd = cmd with { UserId = userId };
        }
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("AssignLeader")]
    public async Task<IActionResult> AssignLeader(OkScheduleOpAssignLeaderCommand cmd)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                cmd = cmd with { UserId = userId };
            }
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        catch (ScheduleWarningException ex)
        {
            return UnprocessableEntity(new JSendFail(new { Warnings = ex.Warnings, RequireOverride = true }));
        }
    }

    [HttpPatch]
    [Route("Cancel")]
    public async Task<IActionResult> CancelSchedule(OkScheduleOpCancelCommand cmd)
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            cmd = cmd with { UserId = userId };
        }
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("ConfirmSchedule")]
    public async Task<IActionResult> ConfirmSchedule(ConfirmScheduleRequest req)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = req.UserId;
            }
            var cmd = new OkScheduleOpConfirmCommand(req.OrderOpId, userId);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        catch (ScheduleCollisionException ex)
        {
            return Conflict(new JSendFail(ex.Message));
        }
    }

    [HttpPost]
    [Route("DiscardDraft")]
    public async Task<IActionResult> DiscardDraft(DiscardDraftRequest req)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = req.UserId;
        }
        var cmd = new OkScheduleOpDiscardDraftCommand(req.OrderOpId, userId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{orderOpId}")]
    public async Task<IActionResult> GetSchedule(string orderOpId)
    {
        var query = new OkScheduleOpGetQuery(orderOpId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    private string GetUserId()
    {
        var user = HttpContext?.User;
        if (user == null)
            return string.Empty;

        return user.FindFirstValue("UserId")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? string.Empty;
    }
}

public record ScheduleOpCancelRequest(string OrderOpId, string AlasanBatal)
{
    public static implicit operator OkScheduleOpCancelCommand(ScheduleOpCancelRequest req) =>
        new(req.OrderOpId, string.Empty, req.AlasanBatal);
}

public record ConfirmScheduleRequest(string OrderOpId, string UserId = "")
{
    public static implicit operator OkScheduleOpConfirmCommand(ConfirmScheduleRequest req) =>
        new(req.OrderOpId, req.UserId);
    public static implicit operator ConfirmScheduleRequest(OkScheduleOpConfirmCommand cmd) =>
        new(cmd.OrderOpId, cmd.UserId);
}

public record DiscardDraftRequest(string OrderOpId, string UserId = "")
{
    public static implicit operator OkScheduleOpDiscardDraftCommand(DiscardDraftRequest req) =>
        new(req.OrderOpId, req.UserId);
    public static implicit operator DiscardDraftRequest(OkScheduleOpDiscardDraftCommand cmd) =>
        new(cmd.OrderOpId, cmd.UserId);
}

public record JSendFail(object Data, string Status = "fail");
