using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/admission")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
public class AdmissionController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdmissionController(IMediator mediator) => _mediator = mediator;

    [HttpPost("from-opname-request")]
    public async Task<IActionResult> ProcessFromOpnameRequest([FromBody] AdmProcessOpnameRequestBody body)
    {
        var cmd = new AdmProcessOpnameRequestCmd(
            body.OpnameRequestId,
            body.KelasDkId,
            body.BangsalId,
            body.UserId,
            body.Registration);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("from-reservation")]
    public async Task<IActionResult> ProcessFromReservation([FromBody] AdmProcessReservationBody body)
    {
        var cmd = new AdmProcessReservationCmd(
            body.ReservationId,
            body.KelasDkId,
            body.BangsalId,
            body.UserId,
            body.Registration);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdmUpdateAdmissionBody body)
    {
        var cmd = new AdmUpdateAdmissionCmd(id, body.KelasDkId, body.BangsalId, body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{regId}/cancel")]
    public async Task<IActionResult> Cancel(string regId, [FromBody] AdmCoordinatedCancelBody? body)
    {
        var correlationId = CorrelationId(body?.RequestId);
        var message = "Invalid request.";
        if (body is null || !IsValid(regId, body, out message))
            return Failure(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", message, correlationId);

        try
        {
            var command = new AdmCoordinatedCancelCmd(
                regId,
                body.Reason!.Trim(),
                body.UserId!.Trim(),
                body.ExpectedAdmissionStatus!.Value,
                body.ExpectedUpdatedAt,
                body.RequestId!.Trim(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.FirstOrDefault());
            var result = await _mediator.Send(command);
            return Ok(new JSendOk(result));
        }
        catch (KeyNotFoundException ex)
        {
            return Failure(StatusCodes.Status404NotFound, "ADMISSION_OR_REGISTRATION_NOT_FOUND", ex.Message, correlationId);
        }
        catch (CoordinatedCancellationException ex)
        {
            var statusCode = ex.Code == CoordinatedCancellationErrorCode.RegistrationHasBillingItems
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status409Conflict;
            var blockers = ex.Code == CoordinatedCancellationErrorCode.RegistrationHasBillingItems
                ? new[] { CoordinatedCancellationErrorCode.RegistrationHasBillingItems }
                : null;
            return Failure(statusCode, ex.Code, ex.Message, correlationId, blockers);
        }
        catch (ArgumentException ex)
        {
            return Failure(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", ex.Message, correlationId);
        }
        catch (Exception)
        {
            return Failure(StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred.", correlationId);
        }
    }

    private static bool IsValid(string regId, AdmCoordinatedCancelBody body, out string message)
    {
        if (string.IsNullOrWhiteSpace(regId) || regId.Trim().Length > 50)
            return Invalid("RegId is required and must be at most 50 characters.", out message);
        if (string.IsNullOrWhiteSpace(body.Reason) || body.Reason.Trim().Length > 500)
            return Invalid("Reason is required and must be 1 to 500 characters.", out message);
        if (string.IsNullOrWhiteSpace(body.UserId) || body.UserId.Trim().Length > 50)
            return Invalid("UserId is required and must be at most 50 characters.", out message);
        if (string.IsNullOrWhiteSpace(body.RequestId) || body.RequestId.Trim().Length > 50)
            return Invalid("RequestId is required and must be 1 to 50 characters.", out message);
        if (!body.ExpectedAdmissionStatus.HasValue || !Enum.IsDefined(body.ExpectedAdmissionStatus.Value))
            return Invalid("ExpectedAdmissionStatus is required and invalid.", out message);
        if (body.ExpectedUpdatedAt.HasValue && body.ExpectedUpdatedAt.Value.Kind != DateTimeKind.Utc)
            return Invalid("ExpectedUpdatedAt must be an ISO-8601 UTC timestamp.", out message);
        message = string.Empty;
        return true;
    }

    private static bool Invalid(string value, out string message) { message = value; return false; }
    private static string? CorrelationId(string? requestId) => string.IsNullOrWhiteSpace(requestId) || requestId.Trim().Length > 50
        ? null : requestId.Trim();
    private static ObjectResult Failure(int statusCode, string code, string message, string? correlationId, string[]? blockers = null) =>
        new(new CancellationJSendFailure("fail", new CancellationErrorData(code, message, blockers, correlationId))) { StatusCode = statusCode };

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new AdmGetAdmissionQry(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(
        [FromQuery] AdmissionStatusEnum? status,
        [FromQuery] string? pasienId)
    {
        var result = await _mediator.Send(new AdmLookupAdmissionQry(status, pasienId));
        return Ok(new JSendOk(result));
    }

    [HttpGet("bangsal")]
    public async Task<IActionResult> ListEligibleBangsal([FromQuery] string kelasDkId)
    {
        var result = await _mediator.Send(new AdmListEligibleBangsalQry(kelasDkId));
        return Ok(new JSendOk(result));
    }
}

public record AdmProcessOpnameRequestBody(
    string OpnameRequestId,
    string KelasDkId,
    string BangsalId,
    string UserId,
    AdmissionRegistrationData Registration);

public record AdmProcessReservationBody(
    string ReservationId,
    string KelasDkId,
    string BangsalId,
    string UserId,
    AdmissionRegistrationData Registration);

public record AdmUpdateAdmissionBody(
    string KelasDkId,
    string BangsalId,
    string UserId);

public record AdmCoordinatedCancelBody(
    string? Reason,
    string? UserId,
    AdmissionStatusEnum? ExpectedAdmissionStatus,
    DateTime? ExpectedUpdatedAt,
    string? RequestId);

public record CancellationJSendFailure(string Status, CancellationErrorData Data);
public record CancellationErrorData(string Code, string Message, string[]? Blockers, string? CorrelationId);
