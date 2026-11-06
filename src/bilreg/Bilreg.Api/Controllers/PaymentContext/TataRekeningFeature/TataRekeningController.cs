using System.Security.Claims;
using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;
using Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature;

/// <summary>
/// Tata Rekening Financial Control API (Simplified 2-Step Workflow).
/// </summary>
[Route("api/tatarekening")]
[ApiController]
[Authorize]
public class TataRekeningController : ControllerBase
{
    private readonly IMediator _mediator;

    public TataRekeningController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private string ResolveUserId(string? userId) =>
        !string.IsNullOrWhiteSpace(userId)
            ? userId
            : (User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value
               ?? User.Identity?.Name
               ?? "admin");

    /// <summary>SOP-TR-01 — Open Tata Rekening workspace for a registration.</summary>
    [HttpGet("{regId}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Open(string regId)
    {
        var result = await _mediator.Send(new OpenTataRekeningQuery(regId));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>2. Close Bill after operational charges are complete.</summary>
    [HttpPost("{regId}/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(string regId)
    {
        var result = await _mediator.Send(new CloseBillCommand(regId));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>3. Execute Merge Billing for a pending merge request.</summary>
    [HttpPost("merge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Merge([FromBody] MergeBillingRequest request)
    {
        var result = await _mediator.Send(new MergeBillingCommand(request.MergeRequestId, ResolveUserId(request.UserId)));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-04 — Financial Verification (verify or require adjustment).</summary>
    [HttpPost("{regId}/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Verify(string regId, [FromBody] FinancialVerificationRequest request)
    {
        var result = await _mediator.Send(
            new FinancialVerificationCommand(regId, request.Action, request.VerifiedAt, ResolveUserId(request.UserId)));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-05 — Apply a financial adjustment to billing or responsibility.</summary>
    [HttpPost("{regId}/adjust")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Adjust(string regId, [FromBody] FinancialAdjustmentRequest request)
    {
        var result = await _mediator.Send(
            new FinancialAdjustmentCommand(regId, request.Adjustment, request.AppliedAt, ResolveUserId(request.UserId)));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-06 — Allocate financial responsibility across payers.</summary>
    [HttpPost("{regId}/allocate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Allocate(
        string regId,
        [FromBody] AllocateFinancialResponsibilityRequest request)
    {
        var result = await _mediator.Send(
            new AllocateFinancialResponsibilityCommand(regId, request.Payments));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-07 — Finalize financial responsibility allocation.</summary>
    [HttpPost("{regId}/finalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Finalize(
        string regId,
        [FromBody] FinalizeFinancialResponsibilityRequest? request)
    {
        var petugas = ResolveUserId(request?.UserId);
        var result = await _mediator.Send(
            new FinalizeFinancialResponsibilityCommand(regId, petugas, request?.FinalizationDate));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-08 — Cancel finalization to allow re-allocation.</summary>
    [HttpPost("{regId}/cancel-finalization")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelFinalization(
        string regId,
        [FromBody] CancelFinalizationRequest request)
    {
        var result = await _mediator.Send(new CancelFinalizationCommand(
            regId, ResolveUserId(request.UserId), request.SupervisorUserId, request.Reason));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-09 — Reopen billing after charge-source adjustment.</summary>
    [HttpPost("{regId}/reopen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reopen(string regId, [FromBody] ReopenBillingRequest request)
    {
        var result = await _mediator.Send(new ReopenBillingCommand(regId, request.Reason, ResolveUserId(request.UserId)));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>5. List summary bills for a registration.</summary>
    [HttpGet("summaryBill/{regId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSummaryBills(string regId)
    {
        var query = new TataRekeningListSummaryBillQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    /// <summary>6. Combined Allocate &amp; Finalize financial responsibility.</summary>
    [HttpPost("{regId}/allocate-finalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AllocateFinalize(
        string regId,
        [FromBody] AllocateFinalizeRequest request)
    {
        var userId = ResolveUserId(request.UserId);
        var result = await _mediator.Send(
            new AllocateFinalizeCommand(regId, request.Payments, userId, request.FinalizationDate));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>7. Cancel finalization to reopen billing for reallocation.</summary>
    [HttpPost("{regId}/cancel-finalize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelFinalize(
        string regId,
        [FromBody] CancelFinalizeRequest request)
    {
        var userId = ResolveUserId(request.UserId);
        var supervisorUserId = !string.IsNullOrWhiteSpace(request.SupervisorUserId)
            ? request.SupervisorUserId
            : userId;
        var result = await _mediator.Send(new CancelFinalizationCommand(regId, userId, supervisorUserId, request.Reason));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>8. Settlement initiation handoff to cashier.</summary>
    [HttpPost("{regId}/settlement-initiation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SettlementInitiation(
        string regId,
        [FromBody] SettlementInitiationRequest? request)
    {
        var petugas = ResolveUserId(request?.UserId);
        var result = await _mediator.Send(
            new SettlementInitiationCommand(regId, petugas, request?.InitiatedAt));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }
}
