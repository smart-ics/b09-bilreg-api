using Bilreg.Application.AdmisiContext.BookingFeature;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/[controller]")]
[ApiController]
public class JadwalPraktekController : ControllerBase
{
    private readonly IMediator _mediator;
    public JadwalPraktekController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(JadwalPraktekCreateCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
    
}