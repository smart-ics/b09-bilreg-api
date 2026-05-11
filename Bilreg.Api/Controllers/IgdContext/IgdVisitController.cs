using Bilreg.Application.IgdContext.BedIgdFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext;

[Route("api/[controller]")]
[ApiController]
public class IgdVisitController : Controller
{
    private readonly IMediator _mediator;

    public IgdVisitController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Daftar(IgdVisitDaftarCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch("{id}/dokter")]
    public async Task<IActionResult> AssignDokter(string id, [FromBody] IgdVisitAssignDokterBody body)
    {
        var cmd = new IgdVisitAssignDokterCmd(id, body.DokterId, body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/triage")]
    public async Task<IActionResult> AssessTriage(string id, [FromBody] IgdVisitAssessTriageBody body)
    {
        var cmd = new IgdVisitAssessTriageCmd(id, body.TriageLevel, body.Notes, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/assignBed")]
    public async Task<IActionResult> AssignBed(string id, [FromBody] IgdAssignBedBody body)
    {
        var cmd = new IgdAssignBedCmd(id, body.BedIgdId, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/checkOut")]
    public async Task<IActionResult> CheckOut(string id, [FromBody] IgdCheckOutBedBody body)
    {
        var cmd = new IgdCheckOutBedCmd(id, body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/redirectRawatJalan")]
    public async Task<IActionResult> RedirectRawatJalan(string id, [FromBody] IgdRedirectRawatJalanBody body)
    {
        var cmd = new IgdVisitRedirectRawatJalanCmd(id, body.Reason, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch("{id}/register")]
    public async Task<IActionResult> AssignRegister(string id, [FromBody] IgdAssignRegisterBody body)
    {
        var cmd = new IgdVisitAssignRegisterCmd(id, body.RegId, body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/discharge")]
    public async Task<IActionResult> Discharge(string id, [FromBody] IgdDischargeBody body)
    {
        var cmd = new IgdVisitDischargeCmd(id, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/void")]
    public async Task<IActionResult> Void(string id, [FromBody] IgdVoidBody body)
    {
        var cmd = new IgdVisitVoidCmd(id, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new IgdVisitGetQuery(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet("aktif")]
    public async Task<IActionResult> ListAktif()
    {
        var result = await _mediator.Send(new IgdVisitListAktifQuery());
        return Ok(new JSendOk(result));
    }
}

public record IgdVisitAssignDokterBody(string DokterId, string UserId);
public record IgdVisitAssessTriageBody(string TriageLevel, string Notes, string UserId);
public record IgdAssignBedBody(string BedIgdId, string UserId);
public record IgdCheckOutBedBody(string UserId);
public record IgdRedirectRawatJalanBody(string Reason, string UserId);
public record IgdAssignRegisterBody(string RegId, string UserId);
public record IgdDischargeBody(string UserId);
public record IgdVoidBody(string UserId);
