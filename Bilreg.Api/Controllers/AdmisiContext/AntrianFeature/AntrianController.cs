using Bilreg.Application.AdmisiContext.AntrianFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[Route("api/[controller]")]
[ApiController]
public class AntrianController : Controller
{
    private readonly IMediator _mediator;

    public AntrianController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("list/{tglAntrian}/{dokterId}")]
    public async Task<IActionResult> ListData(string tglAntrian, string dokterId)
    {
        var query = new AntrianListQuery(tglAntrian, dokterId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("getLastNumber/{dokterId}/{tglPraktek}/{jamMulai}")]
    public async Task<IActionResult> GetLastNumber(string dokterId, string tglPraktek, string jamMulai)
    {
        var query = new AntrianGetLastNumberQuery(dokterId, tglPraktek, jamMulai);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("genNewNumber")]
    public async Task<IActionResult> GetAvailabelNumberQueue(AntrianGenNewNumberCommand cmd)
    {
        var responst = await _mediator.Send(cmd);
        return Ok(new JSendOk(responst));
    }
}
