using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class JadwalPraktekHarianController : ControllerBase
{
    private readonly IMediator _mediator;

    public JadwalPraktekHarianController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{tglPraktek}")]
    public async Task<IActionResult> List(string tglPraktek, [FromQuery] string? dokterId)
    {
        var query = new JadwalPraktekHarianListQuery(tglPraktek, dokterId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("save")]
    public async Task<IActionResult> Save(JadwalPraktekHarianSaveCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("cancel")]
    public async Task<IActionResult> Cancel(JadwalPraktekHarianCancelCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}
