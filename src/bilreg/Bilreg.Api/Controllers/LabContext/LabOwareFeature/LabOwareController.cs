using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabOwareFeature;

[Route("api/LabContext/LabOwareFeature")]
[ApiController]
[Authorize]
public class LabOwareController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabOwareController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue(LabOwareQueueEnqueueCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch("retry")]
    public async Task<IActionResult> Retry(LabOwareRetryCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process(
        [FromQuery] int? batchSize,
        [FromQuery] string? userId)
    {
        var response = await _mediator.Send(new LabOwareQueueProcessCmd(batchSize, userId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] int? queueStatus,
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2)
    {
        var response = await _mediator.Send(new LabOwareQueueWorklistQuery(
            queueStatus, searchTerm, date1, date2));
        return Ok(new JSendOk(response));
    }
}
