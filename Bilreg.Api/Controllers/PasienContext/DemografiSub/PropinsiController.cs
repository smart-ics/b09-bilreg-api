using Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.DemografiSub;

[Route("api/[controller]")]
[ApiController]
public class PropinsiController : ControllerBase
{
    private readonly IMediator _mediator;

    public PropinsiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Save(SukuSaveCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}