using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

[Route("api/[controller]")]
[ApiController]
public class ProsedurMasukInapController : Controller
{
    private readonly IMediator _mediator;
    public ProsedurMasukInapController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetProsedurMasukInap(string id)
    {
        var query = new ProsedurMasukInapGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> ListProsedurMasukInap()
    {
        var query = new ProsedurMasukInapListQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}
