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

    /// <summary>1. Open Tata Rekening workspace for a registration.</summary>
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(new MergeBillingCommand(request.MergeRequestId, userId));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }

    /// <summary>4. Reopen billing after charge-source adjustment.</summary>
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
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
        var userId = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(new CancelFinalizationCommand(regId, userId, request.Reason));
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
        var userId = !string.IsNullOrWhiteSpace(request?.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(
            new SettlementInitiationCommand(regId, userId, request?.InitiatedAt));
        return Ok(new JSendOk(TataRekeningApiMapper.ToApiResponse(result)));
    }
}
