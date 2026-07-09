using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class JenisInapController : Controller
{
    private readonly IMediator _mediator;
    public JenisInapController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetJenisInap(string id)
    {
        var query = new JenisInapGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> ListJenisInap()
    {
        var query = new JenisInapListQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}
