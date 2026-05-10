using Bilreg.Application.IgdContext.BedIgdFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext.IgdVisitFeature;

[Route("api/[controller]")]
[ApiController]
public class BedIgdController : Controller
{
    private readonly IMediator _mediator;

    public BedIgdController(IMediator mediator)
    {
        _mediator = mediator;
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
