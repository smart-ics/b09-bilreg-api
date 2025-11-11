using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.BookingFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/[controller]")]
[ApiController]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(BookingCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("genPasien")]
    public async Task<IActionResult> ResolvePasienId(BokGenPasienFromBookingCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    
    [HttpPatch]
    [Route("resolvePasienId")]
    public async Task<IActionResult> ResolvePasienId(BookingResolvePasienIdCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

}