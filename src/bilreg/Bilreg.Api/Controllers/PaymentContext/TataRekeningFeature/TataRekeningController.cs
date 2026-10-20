using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature;

/// <summary>
/// Tata Rekening Financial Control API (SOP-TR-01 through SOP-TR-05, SOP-TR-09).
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(new MergeBillingCommand(request.MergeRequestId, userId));
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(
            new FinancialVerificationCommand(regId, request.Action, request.VerifiedAt, userId));
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(
            new FinancialAdjustmentCommand(regId, request.Adjustment, request.AppliedAt, userId));
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(new ReopenBillingCommand(regId, request.Reason, userId));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    [HttpGet]
    [Route("summaryBill/{regId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSummaryBills(string regId)
    {
        var query = new TataRekeningListSummaryBillQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}
