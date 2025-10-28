using Bilreg.Application.PasienContext.PasienFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext;

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
    public async Task<IActionResult> Create(PasienCreateCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut]
    [Route("administrativeInfo")]
    public async Task<IActionResult> Create(PasienUpdateAdminInfoCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}