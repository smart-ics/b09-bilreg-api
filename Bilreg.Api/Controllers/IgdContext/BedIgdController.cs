using Bilreg.Application.IgdContext.BedIgdFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext;

[Route("api/[controller]")]
[ApiController]
public class BedIgdController : ControllerBase
{
    private readonly IMediator _mediator;

    public BedIgdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch("{id}/markClean")]
    public async Task<IActionResult> MarkClean(string id, [FromBody] MarkCleanBody body)
    {
        var cmd = new BedIgdMarkCleanCmd(id, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("available")]
    public async Task<IActionResult> ListAvailable()
    {
        var result = await _mediator.Send(new BedIgdListAvailableQuery());
        return Ok(new JSendOk(result));
    }

    [HttpGet("pakaiBed/orphan")]
    public async Task<IActionResult> ListOrphanPakaiBed()
    {
        var result = await _mediator.Send(new PakaiBedListOrphanQuery());
        return Ok(new JSendOk(result));
    }
}

public record MarkCleanBody(string UserId);
