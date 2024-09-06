using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
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
    
    [HttpPost]
    public async Task<IActionResult> Save(PetugasMedisSaveCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("addLayanan")]
    public async Task<IActionResult> AddLayanan(PetugasMedisAddLayananCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("removeLayanan")]
    public async Task<IActionResult> RemoveLayanan(PetugasMedisRemoveLayananCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("addSatTugas")]
    public async Task<IActionResult> AddSatTugas(PetugasMedisAddSatTugasCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("removeSatTugas")]
    public async Task<IActionResult> RemoveSatTugas(PetugasMedisRemoveSatTugasCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut]
    [Route("setSatTugasUtama")]
    public async Task<IActionResult> SetAsSatTugasUtama(SetSatTugasUtamaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut]
    [Route("UnSetSatTugasUtama")]
    public async Task<IActionResult> UnSetAsSatTugasUtama(UnSetSatTugasUtamaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
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