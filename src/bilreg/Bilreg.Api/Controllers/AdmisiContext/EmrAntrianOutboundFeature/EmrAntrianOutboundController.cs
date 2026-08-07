using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.EmrAntrianOutboundFeature;

[Route("api/AdmisiContext/EmrAntrianOutboundFeature")]
[ApiController]
public class EmrAntrianOutboundController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmrAntrianOutboundController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch("retry")]
    public async Task<IActionResult> Retry(EmrAntrianOutboundRetryCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process(
        [FromQuery] int? batchSize,
        [FromQuery] string? userId)
    {
        var response = await _mediator.Send(new EmrAntrianOutboundProcessCmd(batchSize, userId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] int? queueStatus,
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2)
    {
        var response = await _mediator.Send(new EmrAntrianOutboundWorklistQuery(
            queueStatus, searchTerm, date1, date2));
        return Ok(new JSendOk(response));
    }
}
