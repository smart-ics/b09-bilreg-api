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


    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new RegGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list/{tglMasukYmd}/{layananId}")]
    public async Task<IActionResult> ListData(string tglMasukYmd, string layananId)
    {
        var query = new RegListQuery(tglMasukYmd, layananId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}