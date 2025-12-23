using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
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

    [HttpGet]
    [Route("{scheduleOpId}")]
    public async Task<IActionResult> GetOperation(string scheduleOpId)
    {
        var cmd = new OkOnOperationGetQuery(scheduleOpId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}
