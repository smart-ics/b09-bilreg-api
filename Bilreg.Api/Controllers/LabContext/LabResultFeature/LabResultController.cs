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

    [HttpPatch("verify")]
    public async Task<IActionResult> Verify(LabResultVerifyCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("worklist/verification")]
    public async Task<IActionResult> VerificationWorklist(
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2)
    {
        var response = await _mediator.Send(new LabResultVerificationWorklistQuery(searchTerm, date1, date2));
        return Ok(new JSendOk(response));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId)
    {
        var response = await _mediator.Send(new LabResultGetQuery(orderId));
        return Ok(new JSendOk(response));
    }
}
