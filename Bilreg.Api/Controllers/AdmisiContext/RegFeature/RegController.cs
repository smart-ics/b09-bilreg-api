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
     public async Task<IActionResult> Save(RegJalanWalkInCommand cmd)
     {
         await _mediator.Send(cmd);
         return Ok(new JSendOk("Done"));
     }
}