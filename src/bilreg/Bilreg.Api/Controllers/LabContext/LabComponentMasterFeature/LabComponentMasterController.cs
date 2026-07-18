using Bilreg.Application.LabContext.LabComponentMasterFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabComponentMasterFeature;

[Route("api/LabContext/LabComponentMasterFeature")]
[ApiController]
[Authorize]
public class LabComponentMasterController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabComponentMasterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("components")]
    public async Task<IActionResult> ListComponents(
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? search = null)
    {
        var response = await _mediator.Send(new LabComponentMasterListQuery(activeOnly, search));
        return Ok(new JSendOk(response));
    }

    [HttpGet("components/{componentId}")]
    public async Task<IActionResult> GetComponent(string componentId)
    {
        var response = await _mediator.Send(new LabComponentMasterGetQuery(componentId));
        return Ok(new JSendOk(response));
    }
}
