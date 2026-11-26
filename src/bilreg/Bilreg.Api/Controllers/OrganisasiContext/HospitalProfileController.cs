using Bilreg.Application.Shared.HospitalProfileFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.OrganisasiContext;

[Route("api/organisasi")]
[ApiController]
public class HospitalProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public HospitalProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("hospital-profile")]
    public async Task<IActionResult> GetHospitalProfile()
    {
        var response = await _mediator.Send(new HospitalProfileGetQuery());
        return Ok(new JSendOk(response));
    }
}
