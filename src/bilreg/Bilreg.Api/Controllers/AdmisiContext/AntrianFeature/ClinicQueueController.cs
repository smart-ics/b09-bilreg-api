using System.Security.Claims;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[ApiController]
[Route("api/v1/clinic-queue")]
public sealed class ClinicQueueController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserContext? _currentUser;

    public ClinicQueueController(IMediator mediator, ICurrentUserContext? currentUser = null)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("entries/{q}/{n}/call")]
    public async Task<IActionResult> Call(
        string q,
        int n,
        [FromBody] ClinicQueueCallBody body,
        CancellationToken ct)
    {
        body ??= new ClinicQueueCallBody(string.Empty, string.Empty);
        var cmd = new ClinicQueueCallCmd(
            q,
            n,
            body.PoliId,
            body.DokterId,
            body.RuangId,
            ResolveUser(body.UserId));
        var response = await _mediator.Send(cmd, ct);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n}/recall")]
    public async Task<IActionResult> Recall(
        string q,
        int n,
        [FromBody] ClinicQueueRecallBody body,
        CancellationToken ct)
    {
        body ??= new ClinicQueueRecallBody(string.Empty, string.Empty);
        var cmd = new ClinicQueueRecallCmd(
            q,
            n,
            body.PoliId,
            body.DokterId,
            body.RuangId,
            ResolveUser(body.UserId));
        var response = await _mediator.Send(cmd, ct);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n}/skip")]
    public async Task<IActionResult> Skip(
        string q,
        int n,
        [FromBody] ClinicQueueSkipBody body,
        CancellationToken ct)
    {
        body ??= new ClinicQueueSkipBody(string.Empty, string.Empty);
        var cmd = new ClinicQueueSkipCmd(
            q,
            n,
            body.PoliId,
            body.DokterId,
            ResolveUser(body.UserId));
        var response = await _mediator.Send(cmd, ct);
        return Ok(new JSendOk(response));
    }

    [HttpGet("displays/current")]
    [AllowAnonymous]
    public async Task<IActionResult> CurrentDisplay(
        [FromQuery] string? displayId = null,
        [FromQuery(Name = "poliIds")] string[]? poliIds = null,
        [FromQuery(Name = "poliId")] string? poliId = null,
        [FromQuery] string? tanggal = null,
        CancellationToken ct = default)
    {
        var list = new List<string>();
        if (poliIds is not null && poliIds.Length > 0)
        {
            foreach (var item in poliIds)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                var parts = item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                list.AddRange(parts);
            }
        }
        if (!string.IsNullOrWhiteSpace(poliId))
        {
            var parts = poliId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            list.AddRange(parts);
        }

        var distinctPoliIds = list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var query = new ClinicQueueGetDisplaySnapshotQuery(
            displayId,
            distinctPoliIds.Count > 0 ? distinctPoliIds : null,
            tanggal);

        var response = await _mediator.Send(query, ct);
        return Ok(new JSendOk(response));
    }

    private string ResolveUser(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            return userId.Trim();

        try
        {
            var actor = _currentUser?.GetActorUserId();
            if (!string.IsNullOrWhiteSpace(actor))
                return actor;
        }
        catch
        {
        }

        return User?.FindFirstValue(ClaimTypes.Email)
            ?? User?.FindFirstValue(ClaimTypes.Name)
            ?? "system";
    }

    public sealed record ClinicQueueCallBody(
        string PoliId,
        string DokterId,
        string? RuangId = null,
        string? UserId = null);

    public sealed record ClinicQueueRecallBody(
        string PoliId,
        string DokterId,
        string? RuangId = null,
        string? UserId = null);

    public sealed record ClinicQueueSkipBody(
        string PoliId,
        string DokterId,
        string? UserId = null);
}
