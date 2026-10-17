using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BrmContext;

[Route("api/brm/mutasi-berkas")]
[Route("api/brm/[controller]")]
[ApiController]
[Authorize]
public class MutasiBerkasController : ControllerBase
{
    private readonly IMediator _mediator;

    public MutasiBerkasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("kirim")]
    public async Task<IActionResult> Kirim([FromBody] MutasiBerkasKirimCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("terima")]
    public async Task<IActionResult> Terima([FromBody] MutasiBerkasTerimaCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("transfer-unit")]
    public async Task<IActionResult> TransferUnit([FromBody] MutasiBerkasTransferUnitCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("kembali")]
    public async Task<IActionResult> Kembali([FromBody] MutasiBerkasKembaliCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("hilang")]
    public async Task<IActionResult> Hilang([FromBody] MutasiBerkasHilangCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("void")]
    public async Task<IActionResult> Void([FromBody] MutasiBerkasVoidCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("cetak-tracer")]
    public async Task<IActionResult> CetakTracer([FromBody] MutasiBerkasCetakTracerCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] string? tglPraktek,
        [FromQuery] string? layananId,
        [FromQuery] string? sesiPoli,
        [FromQuery] int? status,
        [FromQuery] string? gedungLantai)
    {
        var query = new MutasiBerkasWorklistQuery(tglPraktek, layananId, sesiPoli, status, gedungLantai);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("histori/{mr}")]
    public async Task<IActionResult> Histori(string mr)
    {
        var query = new MutasiBerkasHistoriQuery(mr);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("posisi/{mr}")]
    public async Task<IActionResult> Posisi(string mr)
    {
        var query = new MutasiBerkasPosisiGetQuery(mr);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("lokasi-rak")]
    public async Task<IActionResult> LokasiRak()
    {
        var query = new LokasiBerkasF12Query();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}
