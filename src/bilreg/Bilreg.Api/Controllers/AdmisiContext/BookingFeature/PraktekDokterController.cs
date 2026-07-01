using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.BookingFeature;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;
[Route("api/[controller]")]
[ApiController]
public class PraktekDokterController : Controller
{
    private readonly IMediator _mediator;

    public PraktekDokterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("groupSpesialis")]
    public async Task<IActionResult> PraktekDokterGroupSpesialis(PraktekDokterPeriodeGroupSpesialisListQuery cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("dokter")]
    public async Task<IActionResult> PraktekDokterDokter(PraktekDokterPeriodeDokterListQuery cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}
