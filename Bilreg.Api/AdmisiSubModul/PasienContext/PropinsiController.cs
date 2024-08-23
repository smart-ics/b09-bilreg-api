using Bilreg.Application.AdmPasienContext.SukuAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.AdmisiSubModul.PasienContext;

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