using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.DataSosialPasienSub;

[Route("api/[controller]")]
[ApiController]
public class PasienController : Controller
{
    private readonly IMediator _mediator;

    public PasienController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(PasienCreateCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    
    [HttpPut]
    public async Task<IActionResult> Save(PasienSetContactCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    
}