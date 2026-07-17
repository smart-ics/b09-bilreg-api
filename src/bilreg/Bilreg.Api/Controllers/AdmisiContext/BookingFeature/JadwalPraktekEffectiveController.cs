
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/JadwalPraktekEffective")]
[ApiController]
[Authorize]
public class JadwalPraktekEffectiveController : ControllerBase
{
    private readonly IMediator _mediator;

    public JadwalPraktekEffectiveController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{dokterId}/{tglYmd}")]
    [AllowAnonymous]
    public async Task<IActionResult> List(string dokterId, string tglYmd)
    {
        var result = await _mediator.Send(new JadwalPraktekEffectiveListQuery(dokterId, tglYmd));
        return Ok(new JSendOk(result));
    }

    [HttpGet("{dokterId}/{tglYmd}/{jamMulai}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string dokterId, string tglYmd, string jamMulai)
    {
        var result = await _mediator.Send(
            new JadwalPraktekEffectiveGetQuery(dokterId, tglYmd, jamMulai));
        return Ok(new JSendOk(result));
    }
}
