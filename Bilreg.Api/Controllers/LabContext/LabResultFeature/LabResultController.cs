using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabResultFeature;

[Route("api/LabContext/LabResultFeature")]
[ApiController]
public class LabResultController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabResultController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("record")]
    public async Task<IActionResult> Record(LabResultRecordCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId)
    {
        var response = await _mediator.Send(new LabResultGetQuery(orderId));
        return Ok(new JSendOk(response));
    }
}
