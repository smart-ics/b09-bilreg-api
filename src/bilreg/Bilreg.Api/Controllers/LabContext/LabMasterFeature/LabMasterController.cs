using Bilreg.Application.LabContext.LabMasterFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabMasterFeature;

[Route("api/LabContext/LabMasterFeature")]
[ApiController]
[Authorize]
public class LabMasterController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabMasterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("disciplines")]
    public async Task<IActionResult> ListDisciplines([FromQuery] string? search = null)
    {
        var response = await _mediator.Send(new LabDisciplineListQuery(search));
        return Ok(new JSendOk(response));
    }

    [HttpPost("disciplines")]
    public async Task<IActionResult> SaveDiscipline([FromBody] LabDisciplineSaveCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("test-groups")]
    public async Task<IActionResult> ListTestGroups([FromQuery] string? search = null)
    {
        var response = await _mediator.Send(new LabTestGroupListQuery(search));
        return Ok(new JSendOk(response));
    }

    [HttpPost("test-groups")]
    public async Task<IActionResult> SaveTestGroup([FromBody] LabTestGroupSaveCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}
