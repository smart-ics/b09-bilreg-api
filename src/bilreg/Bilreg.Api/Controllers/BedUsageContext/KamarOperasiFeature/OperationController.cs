using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OperationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("Start")]
    public async Task<IActionResult> StartOp(OkStartOpCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("Finish")]
    public async Task<IActionResult> FinishOp(OkFinishOpCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("CancelStart")]
    public async Task<IActionResult> CancelStart(OkCancelStartOpCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}
