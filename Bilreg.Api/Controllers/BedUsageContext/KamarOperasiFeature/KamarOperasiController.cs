using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
public class KamarOperasiController : ControllerBase
{
    private readonly IMediator _mediator;

    public KamarOperasiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("CreateOrderOpByReg")]
    public async Task<IActionResult> CreateOrderByReg(OkCreateOrderOpByRegCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("CreateOrderOpByPasien")]
    public async Task<IActionResult> CreateOrderByPasien(OkCreateOrderOpByPasienCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}
