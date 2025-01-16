using Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.JaminanSub;

[Route("api/[controller]")]
[ApiController]
public class PolisController : Controller
{
    private readonly IMediator _mediator;

    public PolisController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost]
    public async Task<IActionResult> Save(PolisCreateCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}