using Bilreg.Api.Controllers.PaymentContext.AlokasiPembayaranFeature.Contracts;
using Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.AlokasiPembayaranFeature;

/// <summary>
/// Alokasi Pembayaran &amp; Pertanggungan API (SOP-TR-06 through SOP-TR-08, SOP-TR-10).
/// </summary>
[Route("api/alokasipembayaran")]
[Route("api/tatarekening")]
[ApiController]
[Authorize]
public class AlokasiPembayaranController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlokasiPembayaranController(IMediator mediator)
    {
        _mediator = mediator;
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
        return Ok(new JSendOk(AlokasiPembayaranApiMapper.ToApiResponse(result)));
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
        var petugas = !string.IsNullOrWhiteSpace(request?.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(
            new FinalizeFinancialResponsibilityCommand(regId, petugas, request?.FinalizationDate));
        return Ok(new JSendOk(AlokasiPembayaranApiMapper.ToApiResponse(result)));
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
        var petugas = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(new CancelFinalizationCommand(regId, petugas, request.Reason));
        return Ok(new JSendOk(AlokasiPembayaranApiMapper.ToApiResponse(result)));
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
        var petugas = !string.IsNullOrWhiteSpace(request?.UserId) ? request.UserId : "-";
        var result = await _mediator.Send(
            new SettlementInitiationCommand(regId, petugas, request?.InitiatedAt));
        return Ok(new JSendOk(AlokasiPembayaranApiMapper.ToApiResponse(result)));
    }
}
