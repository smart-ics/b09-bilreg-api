using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

[Route("api/[controller]")]
[ApiController]
public class RegController : Controller
{
    private readonly IMediator _mediator;

    public RegController(IMediator mediator)
    {
        _mediator = mediator;
    }

     [HttpPost]
     [Route("rajalWalkIn")]
     public async Task<IActionResult> Save(RegJalanWalkInCommand cmd)
     {
         var result = await _mediator.Send(cmd);
         return Ok(new JSendOk(result));
     }

     [HttpPost]
     [Route("rajalByBooking")]
     public async Task<IActionResult> Save(RegJalanByBookingCmd cmd)
     {
         var result = await _mediator.Send(cmd);
         return Ok(new JSendOk(result));
     }

}