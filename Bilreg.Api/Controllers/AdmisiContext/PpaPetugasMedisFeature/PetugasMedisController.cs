using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.PpaFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Writers;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.PpaPetugasMedisFeature;

[Route("api/[controller]")]
[ApiController]
public class PetugasMedisController : Controller
{
    private readonly IMediator _mediator;

    public PetugasMedisController(IMediator mediator)
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

    [HttpGet("list/{satTugasId}")]
    public async Task<IActionResult> ListData(string satTugasId)
    {
        var query = new PpaListQuery(satTugasId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{layananId}/list")]
    public async Task<IActionResult> ListDataLayanan(string layananId)
    {
        var query = new PpaLayananListQuery(layananId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("groupSpesialis/list")]
    public async Task<IActionResult> ListGroupSpesialis()
    {
        var query = new PpaListDokterByGroupSpesialisQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}