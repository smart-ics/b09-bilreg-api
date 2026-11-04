using Bilreg.Application.LabContext.LabExternalFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabExternalFeature;

[Route("api/LabContext/LabExternalFeature")]
[ApiController]
[Authorize]
public class LabExternalController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabExternalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("")]
    [HttpPost("create")]
    public async Task<IActionResult> Create(LabExternalCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(LabExternalCancelCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("{regExternalId}")]
    public async Task<IActionResult> Get([FromRoute] string regExternalId)
    {
        var response = await _mediator.Send(new LabExternalGetQuery(regExternalId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2,
        [FromQuery] int? status)
    {
        var response = await _mediator.Send(new LabExternalListQuery(searchTerm, date1, date2, status));
        return Ok(new JSendOk(response));
    }
}
