using Bilreg.Api.Filters;
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

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] AdmCancelAdmissionBody body)
    {
        await _mediator.Send(new AdmCancelAdmissionCmd(id, body.UserId));
        return Ok(new JSendOk("Done"));
    }

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

public record AdmCancelAdmissionBody(string UserId);
