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
}