using Bilreg.Application.AdmisiContext.BookingFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/[controller]")]
[ApiController]
public class RuangController : Controller
{
    private readonly IMediator _mediator;
    public RuangController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{ruangId}")]
    public async Task<IActionResult> GetData(string ruangId)
    {
        var query = new RuangGetCmd(ruangId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> ListData()
    {
        var query = new RuangListCmd();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
