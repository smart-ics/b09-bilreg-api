using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class IgdVisitSmassTaskController : ControllerBase
{
    private readonly IMediator _mediator;

    public IgdVisitSmassTaskController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{igdVisitId}")]
    public async Task<IActionResult> ListByVisit(string igdVisitId)
    {
        var result = await _mediator.Send(new IgdVisitListSmassTaskQuery(igdVisitId));
        return Ok(new JSendOk(result));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist()
    {
        var result = await _mediator.Send(new IgdVisitSmassWorklistQuery());
        return Ok(new JSendOk(result));
    }

    [HttpPatch("retry")]
    public async Task<IActionResult> Retry([FromBody] IgdVisitSmassTaskRetryBody body)
    {
        var result = await _mediator.Send(new IgdVisitSmassTaskRetryCmd(body.IgdVisitSmassTaskId));
        return Ok(new JSendOk(result));
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process()
    {
        var result = await _mediator.Send(new IgdVisitSmassTaskProcessCmd());
        return Ok(new JSendOk(result));
    }
}

public record IgdVisitSmassTaskRetryBody(string IgdVisitSmassTaskId);
