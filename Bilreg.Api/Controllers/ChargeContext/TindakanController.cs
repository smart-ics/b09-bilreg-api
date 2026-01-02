using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/[controller]")]
[ApiController]
public class TindakanController : Controller
{
    private readonly IMediator _mediator;

    public TindakanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateByKtp(TdkCreateTindakanCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    
}