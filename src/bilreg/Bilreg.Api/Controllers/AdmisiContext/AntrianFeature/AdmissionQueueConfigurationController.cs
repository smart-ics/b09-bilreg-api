using Bilreg.Api.Authorization;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using System.Security.Claims;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[ApiController]
//[Authorize(Policy = AdmissionQueueConfigurationPolicies.PolicyName)]
[Route("api/v1/admission-queue/configuration")]
public sealed class AdmissionQueueConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserContext _currentUser;

    public AdmissionQueueConfigurationController(IMediator mediator, ICurrentUserContext currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role)
            .Concat(User.FindAll("role"))
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new JSendOk(new
        {
            permission = AdmissionQueueConfigurationPolicies.PermissionId,
            email,
            userName = name,
            roles
        }));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetConfigurationSummaryQry(), ct)));

    [HttpGet("workstations")]
    public async Task<IActionResult> ListWorkstations(CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new ListWorkstationsQry(), ct)));

    [HttpGet("workstations/{workstationKey}")]
    public async Task<IActionResult> GetWorkstation(string workstationKey, CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetWorkstationQry(workstationKey), ct)));

    [HttpPost("workstations")]
    public async Task<IActionResult> CreateWorkstation([FromBody] CreateWorkstationBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWorkstationCmd(
            body.WorkstationKey, body.DisplayName, body.LocationName, body.LoketKey,
            body.Active, body.Notes, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPut("workstations/{workstationKey}")]
    public async Task<IActionResult> UpdateWorkstation(
        string workstationKey, [FromBody] UpdateWorkstationBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateWorkstationCmd(
            workstationKey, body.DisplayName, body.LocationName, body.LoketKey,
            body.Notes, body.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("workstations/{workstationKey}/activate")]
    public async Task<IActionResult> ActivateWorkstation(
        string workstationKey, [FromBody] RowVersionBody? body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetWorkstationActiveCmd(
            workstationKey, true, body?.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("workstations/{workstationKey}/deactivate")]
    public async Task<IActionResult> DeactivateWorkstation(
        string workstationKey, [FromBody] RowVersionBody? body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetWorkstationActiveCmd(
            workstationKey, false, body?.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpGet("displays")]
    public async Task<IActionResult> ListDisplays(CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new ListDisplaysQry(), ct)));

    [HttpGet("displays/{displayId}")]
    public async Task<IActionResult> GetDisplay(string displayId, CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetDisplayQry(displayId), ct)));

    [HttpPost("displays")]
    public async Task<IActionResult> CreateDisplay([FromBody] CreateDisplayBody body, CancellationToken ct)
    {
        var lokets = (body.Lokets ?? [])
            .Select(x => new AdmissionDisplayLoketDtoResponse(x.LoketKey, x.SortOrder))
            .ToList();
        var result = await _mediator.Send(new CreateDisplayCmd(
            body.DisplayId, body.DisplayName, body.LocationName, body.Active, body.AudioEnabled,
            body.PollIntervalMs, body.LayoutKey, body.Notes, lokets, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPut("displays/{displayId}")]
    public async Task<IActionResult> UpdateDisplay(string displayId, [FromBody] UpdateDisplayBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDisplayCmd(
            displayId, body.DisplayName, body.LocationName, body.AudioEnabled, body.PollIntervalMs,
            body.LayoutKey, body.Notes, body.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPut("displays/{displayId}/lokets")]
    public async Task<IActionResult> ReplaceLokets(
        string displayId, [FromBody] ReplaceLoketsBody body, CancellationToken ct)
    {
        var lokets = (body.Lokets ?? [])
            .Select(x => new AdmissionDisplayLoketDtoResponse(x.LoketKey, x.SortOrder))
            .ToList();
        var result = await _mediator.Send(new ReplaceDisplayLoketsCmd(
            displayId, lokets, body.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("displays/{displayId}/activate")]
    public async Task<IActionResult> ActivateDisplay(
        string displayId, [FromBody] RowVersionBody? body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetDisplayActiveCmd(
            displayId, true, body?.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("displays/{displayId}/deactivate")]
    public async Task<IActionResult> DeactivateDisplay(
        string displayId, [FromBody] RowVersionBody? body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetDisplayActiveCmd(
            displayId, false, body?.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpGet("kiosks")]
    public async Task<IActionResult> ListKiosks(CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new ListKiosksQry(), ct)));

    [HttpGet("kiosks/{stationId}")]
    public async Task<IActionResult> GetKiosk(string stationId, CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetKioskQry(stationId), ct)));

    [HttpPost("kiosks")]
    public async Task<IActionResult> CreateKiosk([FromBody] CreateKioskBody body, CancellationToken ct)
    {
        var servicePoints = (body.ServicePoints ?? [])
            .Select(x => new AdmissionKioskServicePointDtoResponse(x.ServicePointId, x.SortOrder))
            .ToList();
        var result = await _mediator.Send(new CreateKioskCmd(
            body.StationId, body.DisplayName, body.LocationName, body.Active,
            body.PrinterProxyPort, body.Notes, servicePoints, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPut("kiosks/{stationId}")]
    public async Task<IActionResult> UpdateKiosk(
        string stationId,
        [FromBody] UpdateKioskBody body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateKioskCmd(
            stationId, body.DisplayName, body.LocationName, body.PrinterProxyPort,
            body.Notes, body.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPut("kiosks/{stationId}/service-points")]
    public async Task<IActionResult> ReplaceKioskServicePoints(
        string stationId,
        [FromBody] ReplaceKioskServicePointsBody body,
        CancellationToken ct)
    {
        var servicePoints = (body.ServicePoints ?? [])
            .Select(x => new AdmissionKioskServicePointDtoResponse(x.ServicePointId, x.SortOrder))
            .ToList();
        var result = await _mediator.Send(new ReplaceKioskServicePointsCmd(
            stationId, servicePoints, body.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("kiosks/{stationId}/activate")]
    public async Task<IActionResult> ActivateKiosk(
        string stationId,
        [FromBody] RowVersionBody? body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SetKioskActiveCmd(
            stationId, true, body?.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpPost("kiosks/{stationId}/deactivate")]
    public async Task<IActionResult> DeactivateKiosk(
        string stationId,
        [FromBody] RowVersionBody? body,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SetKioskActiveCmd(
            stationId, false, body?.RowVersion, Actor()), ct);
        return Ok(new JSendOk(result));
    }

    [HttpGet("segmentation")]
    public async Task<IActionResult> Segmentation(CancellationToken ct) =>
        Ok(new JSendOk(await _mediator.Send(new GetSegmentationQry(), ct)));

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(new JSendOk(await _mediator.Send(new ListConfigurationAuditQry(page, pageSize), ct)));

    private string Actor()
    {
        try
        {
            return _currentUser.GetActorUserId();
        }
        catch
        {
            return User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? "unknown";
        }
    }

    public sealed record CreateWorkstationBody(
        string WorkstationKey,
        string DisplayName,
        string? LocationName,
        string LoketKey,
        bool Active = true,
        string? Notes = null);

    public sealed record UpdateWorkstationBody(
        string DisplayName,
        string? LocationName,
        string LoketKey,
        string RowVersion,
        string? Notes = null);

    public sealed record CreateDisplayBody(
        string DisplayId,
        string DisplayName,
        string? LocationName,
        bool Active = true,
        bool AudioEnabled = true,
        int PollIntervalMs = 15000,
        string? LayoutKey = null,
        string? Notes = null,
        List<LoketMapBody>? Lokets = null);

    public sealed record UpdateDisplayBody(
        string DisplayName,
        string? LocationName,
        bool AudioEnabled,
        int PollIntervalMs,
        string RowVersion,
        string? LayoutKey = null,
        string? Notes = null);

    public sealed record ReplaceLoketsBody(string RowVersion, List<LoketMapBody>? Lokets);
    public sealed record LoketMapBody(string LoketKey, int SortOrder = 0);
    public sealed record CreateKioskBody(
        string StationId,
        string DisplayName,
        string? LocationName,
        bool Active = true,
        int PrinterProxyPort = 5050,
        string? Notes = null,
        List<KioskServicePointMapBody>? ServicePoints = null);
    public sealed record UpdateKioskBody(
        string DisplayName,
        string? LocationName,
        int PrinterProxyPort,
        string RowVersion,
        string? Notes = null);
    public sealed record ReplaceKioskServicePointsBody(
        string RowVersion,
        List<KioskServicePointMapBody>? ServicePoints);
    public sealed record KioskServicePointMapBody(string ServicePointId, int SortOrder = 0);
    public sealed record RowVersionBody(string? RowVersion);
}
