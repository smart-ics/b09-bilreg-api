using Bilreg.Application.RadContext.RadJadwalFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.RadContext;

[Route("api/rad/equipment")]
[ApiController]
[Authorize]
public class RadEquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public RadEquipmentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(
        [FromQuery] string? modality = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var response = await _mediator.Send(new RadEquipmentLookupQuery(modality, activeOnly), ct);
        return Ok(new JSendOk(response));
    }
}
