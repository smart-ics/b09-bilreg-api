using Bilreg.Application.AdmisiRanapContext.RolloutFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/rollout")]
[ApiController]
[Authorize]
public class AdmisiRanapRolloutController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdmisiRanapRolloutController(IMediator mediator) => _mediator = mediator;

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _mediator.Send(new AdmGetRolloutStatusQry());
        return Ok(new JSendOk(result));
    }
}
