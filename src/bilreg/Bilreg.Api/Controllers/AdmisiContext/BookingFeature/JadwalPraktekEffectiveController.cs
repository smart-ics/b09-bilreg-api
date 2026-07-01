
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/JadwalPraktekEffective")]
[ApiController]
public class JadwalPraktekEffectiveController : ControllerBase
{
    private readonly IMediator _mediator;

    public JadwalPraktekEffectiveController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{dokterId}/{tglYmd}")]
    public async Task<IActionResult> List(string dokterId, string tglYmd)
    {
        var result = await _mediator.Send(new JadwalPraktekEffectiveListQuery(dokterId, tglYmd));
        return Ok(new JSendOk(result));
    }

    [HttpGet("{dokterId}/{tglYmd}/{jamMulai}")]
    public async Task<IActionResult> Get(string dokterId, string tglYmd, string jamMulai)
    {
        var result = await _mediator.Send(
            new JadwalPraktekEffectiveGetQuery(dokterId, tglYmd, jamMulai));
        return Ok(new JSendOk(result));
    }
}
