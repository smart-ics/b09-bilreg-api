using Bilreg.Api.Authorization;
using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature;

/// <summary>
/// Tata Rekening Financial Control API (SOP-TR-01 through SOP-TR-10).
/// </summary>
[Route("api/tatarekening")]
[ApiController]
[Authorize(Policy = TataRekeningPolicies.Verifikator)]
public class TataRekeningController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserContext _currentUser;

    public TataRekeningController(IMediator mediator, ICurrentUserContext currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

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

    /// <summary>SOP-TR-02 — Close Bill after operational charges are complete.</summary>
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

    /// <summary>SOP-TR-03 — Execute Merge Billing for a pending merge request.</summary>
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
        var result = await _mediator.Send(new MergeBillingCommand(request.MergeRequestId));
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
        var petugasVerif = _currentUser.GetActorUserId();
        var verifiedAt = request.VerifiedAt ?? DateTime.UtcNow;
        var result = await _mediator.Send(
            new FinancialVerificationCommand(regId, request.Action, petugasVerif, verifiedAt));
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
        var appliedAt = request.AppliedAt ?? DateTime.UtcNow;
        var result = await _mediator.Send(
            new FinancialAdjustmentCommand(regId, request.Adjustment, appliedAt));
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
        var petugasVerif = _currentUser.GetActorUserId();
        var finalizationDate = request?.FinalizationDate ?? DateTime.UtcNow;
        var result = await _mediator.Send(
            new FinalizeFinancialResponsibilityCommand(regId, petugasVerif, finalizationDate));
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
        var result = await _mediator.Send(new CancelFinalizationCommand(regId, request.Reason));
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
        var result = await _mediator.Send(new ReopenBillingCommand(regId, request.Reason));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>SOP-TR-10 — Initiate settlement handoff to cashier.</summary>
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
        var petugasVerif = _currentUser.GetActorUserId();
        var initiatedAt = request?.InitiatedAt ?? DateTime.UtcNow;
        var result = await _mediator.Send(
            new SettlementInitiationCommand(regId, petugasVerif, initiatedAt));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }
}
