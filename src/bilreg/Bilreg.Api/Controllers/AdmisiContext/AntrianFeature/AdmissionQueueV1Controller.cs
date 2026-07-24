using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Api.Configurations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[ApiController]
[Authorize]
[Route("api/v1/admission-queue")]
public sealed class AdmissionQueueV1Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AdmissionQueueApiOptions _options;

    public AdmissionQueueV1Controller(
        IMediator mediator,
        IOptions<AdmissionQueueApiOptions>? options = null)
    {
        _mediator = mediator;
        _options = options?.Value ?? new AdmissionQueueApiOptions();
    }

    [HttpGet("service-points")]
    public async Task<IActionResult> ListServicePoints([FromQuery] bool activeOnly = true)
    {
        var query = new AdmissionServicePointListQry(activeOnly);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPut("service-points/{id}")]
    public async Task<IActionResult> UpsertServicePoint(string id, [FromBody] ServicePointBody b)
    {
        var cmd = new AdmissionServicePointUpsertCmd(id, b.DisplayName, b.QueuePrefix, b.Active);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("intake")]
    public async Task<IActionResult> Intake([FromBody] IntakeBody b)
    {
        var cmd = new QueAnonymousIntakeCmd(b.ServicePointId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("booking-assistance")]
    public async Task<IActionResult> BookingAssistance([FromBody] BookingAssistanceBody b)
    {
        var cmd = new BookingAssistanceIntakeCmd(
            b.BookingId, b.ServicePointId, b.FailureCode, b.KioskId, b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] string businessDate,
        [FromQuery] string? servicePointId,
        [FromQuery] int? queueStatus,
        [FromQuery] string? loketKey,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100)
    {
        var query = new AdmissionQueueOfficerWorklistQry(
            businessDate, servicePointId, queueStatus, loketKey, offset, limit);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("displays/current")]
    public async Task<IActionResult> Display([FromQuery] string? loketKey = null)
    {
        var query = new CurrentLoketDisplaySnapshotQry(loketKey);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("rollout/status")]
    public async Task<IActionResult> RolloutStatus()
    {
        var query = new AdmissionQueueGetRolloutStatusQry();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/call")]
    public async Task<IActionResult> Call(string q, int n, [FromBody] ActorLoketBody b)
    {
        var cmd = new AdmissionQueueCallCmd(q, n, Loket(b.LoketKey), b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/recall")]
    public async Task<IActionResult> Recall(string q, int n, [FromBody] VersionedActorLoketBody b)
    {
        var cmd = new AdmissionQueueRecallCmd(
            q, n, Loket(b.LoketKey), Version(b.ExpectedRowVersion), b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/start-service")]
    public async Task<IActionResult> Start(string q, int n, [FromBody] VersionedActorLoketBody b)
    {
        var cmd = new AdmissionQueueStartServiceCmd(
            q, n, Loket(b.LoketKey), Version(b.ExpectedRowVersion), b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/withdraw")]
    public async Task<IActionResult> Withdraw(string q, int n, [FromBody] WithdrawBody b)
    {
        var cmd = new AdmissionQueueWithdrawCmd(
            q,
            n,
            b.Reason,
            b.LoketKey is null ? null : Loket(b.LoketKey),
            OptionalVersion(b.ExpectedRowVersion),
            b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/no-show")]
    public async Task<IActionResult> NoShow(string q, int n, [FromBody] VersionedActorLoketBody b)
    {
        var cmd = new AdmissionQueueNoShowCmd(
            q, n, Loket(b.LoketKey), Version(b.ExpectedRowVersion), b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/redirect")]
    public async Task<IActionResult> Redirect(string q, int n, [FromBody] RedirectBody b)
    {
        var cmd = new AdmissionQueueRedirectCmd(
            q,
            n,
            b.TargetServicePointId,
            b.LoketKey is null ? null : Loket(b.LoketKey),
            OptionalVersion(b.ExpectedRowVersion),
            b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/outcomes/established")]
    public async Task<IActionResult> Established(string q, int n, [FromBody] EstablishedBody b)
    {
        var cmd = new FinalizeRegistrationEstablishedCmd(
            q, n, Loket(b.LoketKey), Version(b.ExpectedRowVersion), b.RegId, b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("entries/{q}/{n:int}/outcomes/not-established")]
    public async Task<IActionResult> NotEstablished(string q, int n, [FromBody] NotEstablishedBody b)
    {
        var cmd = new FinalizeRegistrationNotEstablishedCmd(
            q, n, Loket(b.LoketKey), Version(b.ExpectedRowVersion), b.ReasonCode, b.UserId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    private static byte[] Version(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new ArgumentException("ExpectedRowVersion must be Base64.");
        }
    }

    private static byte[]? OptionalVersion(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Version(value);
    }

    private string Loket(string payload)
    {
        var configured = Request.Headers["X-Loket-Key"].FirstOrDefault();
        var workstationKey = Request.Headers["X-Workstation-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(configured))
            throw new ArgumentException("X-Loket-Key workstation context is required.");
        if (string.IsNullOrWhiteSpace(workstationKey))
            throw new ArgumentException("X-Workstation-Key is required.");
        if (!string.Equals(configured.Trim(), payload?.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("LoketKey does not match workstation context.");

        var workstation = _options.Workstations.SingleOrDefault(x =>
            string.Equals(x.WorkstationKey?.Trim(), workstationKey.Trim(), StringComparison.OrdinalIgnoreCase));
        if (workstation is null)
            throw new ArgumentException("Workstation is not configured for Admission Queue operations.");
        if (!string.Equals(workstation.LoketKey?.Trim(), configured.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Workstation is not configured for the requested LoketKey.");

        return configured.Trim();
    }
}

public record ServicePointBody(string DisplayName, string QueuePrefix, bool Active);
public record IntakeBody(string ServicePointId);
public record BookingAssistanceBody(
    string BookingId, string ServicePointId, string? FailureCode, string KioskId, string UserId);
public record ActorLoketBody(string LoketKey, string UserId);
public record VersionedActorLoketBody(string LoketKey, string ExpectedRowVersion, string UserId);
public record WithdrawBody(string Reason, string? LoketKey, string? ExpectedRowVersion, string UserId);
public record RedirectBody(
    string TargetServicePointId, string? LoketKey, string? ExpectedRowVersion, string UserId);
public record EstablishedBody(string LoketKey, string ExpectedRowVersion, string RegId, string UserId);
public record NotEstablishedBody(
    string LoketKey, string ExpectedRowVersion, string ReasonCode, string UserId);
