using Bilreg.Application.IgdContext.BhpIgdFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext;

[Route("api/[controller]")]
[ApiController]
public class BhpIgdController : Controller
{
    private readonly IMediator _mediator;

    public BhpIgdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{visitId}")]
    public async Task<IActionResult> AddBhp(string visitId, [FromBody] AddBhpBody body)
    {
        var cmd = new IgdBhpAddCmd(visitId, body.BhpItemId, body.BhpItemName, body.Qty, body.Price, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
}

public record AddBhpBody(string BhpItemId, string BhpItemName, int Qty, decimal Price, string UserId);
