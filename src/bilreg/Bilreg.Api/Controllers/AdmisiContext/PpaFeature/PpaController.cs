using Bilreg.Application.AdmisiContext.PpaFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.PpaFeature;

[Route("api/[controller]")]
[ApiController]
public class PpaController : Controller
{
    private readonly IMediator _mediator;

    public PpaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new PpaGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("dokterRajal")]
    public async Task<IActionResult> ListDokterRajal()
    {
        var query = new PpaListDokterRajalQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("dokterOk")]
    public async Task<IActionResult> ListDokterOk()
    {
        var query = new PpaListDokterOkQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("dokterAnestesi")]
    public async Task<IActionResult> ListDokterAnestesi()
    {
        var query = new PpaListDokterAnestesiQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("dokter/{layananId}")]
    public async Task<IActionResult> ListDokterByLayanan(string layananId)
    {
        var query = new PpaListDokterByLayananQuery(layananId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
    [HttpGet("dokter")]
    public async Task<IActionResult> ListDokter()
    {
        var query = new PpaListDokterQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> ListPpa([FromQuery] IEnumerable<string> listSatTugas)
    {
        var query = new PpaListBySatTugasQuery(listSatTugas);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
}