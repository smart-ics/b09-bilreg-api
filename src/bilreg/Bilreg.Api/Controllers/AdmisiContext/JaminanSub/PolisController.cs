using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.JaminanSub;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PolisController : Controller
{
    private readonly IMediator _mediator;

    public PolisController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost]
    public async Task<IActionResult> Save(PolisCreateCommand cmd)
    {
        var response =  await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPut]
    [Route("addCoverage")]
    public async Task<IActionResult> AddCoverage(PolisAddCoverageCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPut]
    [Route("removeCoverage")]
    public async Task<IActionResult> RemoveCoverage(PolisRemoveCoverageCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new PolisGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list/{pasienId}")]
    public async Task<IActionResult> ListData(string pasienId)
    {
        var query = new PolisListQuery(pasienId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("byNoPeserta")]
    public async Task<IActionResult> GetByNoPeserta([FromQuery] string noPeserta)
    {
        var query = new PolisGetByNoPesertaQuery(noPeserta);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }


}