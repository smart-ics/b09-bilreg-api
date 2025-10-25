using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
using Bilreg.Application.AdmisiContext.PetugasMedisSub;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.PetugasMedisSub;

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
        var query = new PetugasMedisGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListData()
    {
        var query = new PetugasMedisListQuery();
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

}