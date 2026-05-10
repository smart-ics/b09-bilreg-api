using Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext;

[Route("api/[controller]")]
[ApiController]
public class TindakanIgdController : Controller
{
    private readonly IMediator _mediator;

    public TindakanIgdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{visitId}")]
    public async Task<IActionResult> AddTindakan(string visitId, [FromBody] AddTindakanBody body)
    {
        var cmd = new IgdTindakanAddCmd(visitId, body.TarifId, body.TarifName, body.Qty, body.Price, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
}

public record AddTindakanBody(string TarifId, string TarifName, int Qty, decimal Price, string UserId);
