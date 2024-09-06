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
    [Route("AddLayanan")]
    public async Task<IActionResult> AddLayanan(PetugasMedisAddLayananCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("RemoveLayanan")]
    public async Task<IActionResult> RemoveLayanan(PetugasMedisRemoveLayananCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("AddSatTugas")]
    public async Task<IActionResult> AddSatTugas(PetugasMedisAddSatTugasCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("RemoveSatTugas")]
    public async Task<IActionResult> RemoveSatTugas(PetugasMedisRemoveSatTugasCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut]
    [Route("SetAsSatTugasUtama")]
    public async Task<IActionResult> SetAsSatTugasUtama(SetSatTugasUtamaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut]
    [Route("UnSetAsSatTugasUtama")]
    public async Task<IActionResult> UnSetAsSatTugasUtama(UnSetSatTugasUtamaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetData(string id, CancellationToken cancellationToken)
    {
        var query = new PetugasMedisGetQuery(id);
        var response = await _mediator.Send(query, cancellationToken);

        if (response == null)
        {
            return NotFound($"Petugas Medis with ID {id} not found.");
        }

        return Ok(response);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetPetugasMedisList(CancellationToken cancellationToken)
    {
        var query = new PetugasMedisListQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

}