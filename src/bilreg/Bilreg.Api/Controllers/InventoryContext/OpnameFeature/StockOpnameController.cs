using System.Security.Claims;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.InventoryContext.OpnameFeature;

public record CreateSessionRequest(
    string LayananId,
    SessionScopeEnum ScopeMode,
    DateTime CutoffDate,
    string? CreatedBy = null,
    IEnumerable<CreateSessionScopeItemRequest>? ScopeItems = null,
    string? SessionId = null);

public record SubmitCountingRequest(
    string? SubmittedBy = null,
    DateTime? SubmittedAt = null,
    Dictionary<string, decimal>? ItemHppMap = null);

public record RecordRecountRequest(
    string VarianceItemId,
    decimal RecountQty,
    DiscrepancyReasonEnum? DiscrepancyReason,
    string Notes,
    string? RecountedBy = null,
    DateTime? RecountTime = null);

public record BulkVerifyMinorRequest(
    decimal MaxNominalThreshold,
    DiscrepancyReasonEnum StandardReason,
    string Notes,
    string? VerifiedBy = null,
    DateTime? VerifiedAt = null);

public record SubmitVerificationRequest(
    string? VerifiedBy = null,
    DateTime? VerifiedAt = null);

public record ApproveSessionRequest(
    string? ApprovedBy = null,
    DateTime? ApprovedAt = null);

public record RejectSessionRequest(
    RejectionDispositionEnum Disposition,
    string Notes,
    string? RejectedBy = null,
    DateTime? RejectedAt = null);

public record CancelSessionRequest(
    string Reason,
    string? CancelledBy = null,
    DateTime? CancelledAt = null);

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockOpnameController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockOpnameController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(
        [FromQuery] string? layananId = null,
        [FromQuery] SessionStatusEnum? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetSessionListQuery(layananId, status, fromDate, toDate);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest req)
    {
        var createdBy = !string.IsNullOrWhiteSpace(req.CreatedBy)
            ? req.CreatedBy
            : GetCurrentUserId();
        var cmd = new CreateSessionCommand(
            req.LayananId,
            req.ScopeMode,
            req.CutoffDate,
            createdBy,
            req.ScopeItems,
            req.SessionId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("session/{id}")]
    public async Task<IActionResult> GetSessionDetail(string id)
    {
        var query = new GetSessionDetailQuery(id);
        var response = await _mediator.Send(query);
        if (response is null)
            return NotFound(new JSendFailed(new KeyNotFoundException($"Stock opname session with ID '{id}' not found.")));
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/start")]
    public async Task<IActionResult> StartCounting(string id)
    {
        var cmd = new StartCountingCommand(id);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/submit-counting")]
    public async Task<IActionResult> SubmitCounting(string id, [FromBody] SubmitCountingRequest? req = null)
    {
        var submittedBy = !string.IsNullOrWhiteSpace(req?.SubmittedBy)
            ? req.SubmittedBy
            : GetCurrentUserId();
        var cmd = new SubmitCountingCommand(id, submittedBy, req?.SubmittedAt, req?.ItemHppMap);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/start-verification")]
    public async Task<IActionResult> StartVerification(string id)
    {
        var cmd = new StartVerificationCommand(id);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("session/{id}/variance-review")]
    public async Task<IActionResult> GetVarianceReview(string id)
    {
        var query = new GetVarianceReviewQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/recount")]
    public async Task<IActionResult> RecordRecount(string id, [FromBody] RecordRecountRequest req)
    {
        var recountedBy = !string.IsNullOrWhiteSpace(req.RecountedBy)
            ? req.RecountedBy
            : GetCurrentUserId();
        var cmd = new RecordRecountCommand(
            id,
            req.VarianceItemId,
            req.RecountQty,
            req.DiscrepancyReason,
            req.Notes,
            recountedBy,
            req.RecountTime);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/bulk-verify-minor")]
    public async Task<IActionResult> BulkVerifyMinor(string id, [FromBody] BulkVerifyMinorRequest req)
    {
        var verifiedBy = !string.IsNullOrWhiteSpace(req.VerifiedBy)
            ? req.VerifiedBy
            : GetCurrentUserId();
        var cmd = new BulkVerifyMinorVariancesCommand(
            id,
            req.MaxNominalThreshold,
            req.StandardReason,
            req.Notes,
            verifiedBy,
            req.VerifiedAt);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/submit-verification")]
    public async Task<IActionResult> SubmitVerification(string id, [FromBody] SubmitVerificationRequest? req = null)
    {
        var verifiedBy = !string.IsNullOrWhiteSpace(req?.VerifiedBy)
            ? req.VerifiedBy
            : GetCurrentUserId();
        var cmd = new SubmitVerificationCommand(id, verifiedBy, req?.VerifiedAt);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("session/{id}/result-report")]
    public async Task<IActionResult> GetResultReport(string id)
    {
        var query = new GetSessionResultReportQuery(id);
        var response = await _mediator.Send(query);
        if (response is null)
            return NotFound(new JSendFailed(new KeyNotFoundException($"Stock opname result report for session '{id}' not found.")));
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/approve")]
    public async Task<IActionResult> ApproveSession(string id, [FromBody] ApproveSessionRequest? req = null)
    {
        var approvedBy = !string.IsNullOrWhiteSpace(req?.ApprovedBy)
            ? req.ApprovedBy
            : GetCurrentUserId();
        var cmd = new ApproveSessionCommand(id, approvedBy, req?.ApprovedAt);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/reject")]
    public async Task<IActionResult> RejectSession(string id, [FromBody] RejectSessionRequest req)
    {
        var rejectedBy = !string.IsNullOrWhiteSpace(req.RejectedBy)
            ? req.RejectedBy
            : GetCurrentUserId();
        var cmd = new RejectSessionCommand(id, req.Disposition, req.Notes, rejectedBy, req.RejectedAt);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("session/{id}/cancel")]
    public async Task<IActionResult> CancelSession(string id, [FromBody] CancelSessionRequest req)
    {
        var cancelledBy = !string.IsNullOrWhiteSpace(req.CancelledBy)
            ? req.CancelledBy
            : GetCurrentUserId();
        var cmd = new CancelSessionCommand(id, req.Reason, cancelledBy, req.CancelledAt);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.Identity?.Name
            ?? "SYSTEM";
    }
}
