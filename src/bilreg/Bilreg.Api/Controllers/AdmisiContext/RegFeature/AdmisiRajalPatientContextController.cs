using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

[ApiController]
[Authorize]
[Route("api/v1/admisi-rajal")]
public sealed class AdmisiRajalPatientContextController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdmisiRajalPatientContextController(IMediator mediator) => _mediator = mediator;

    [HttpPost("patient-context-search")]
    public async Task<IActionResult> Search([FromBody] AdmisiRajalPatientContextSearchQuery query)
    {
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("patient-context/{kind}/{id}")]
    public async Task<IActionResult> Get(
        PatientContextKind kind,
        string id,
        [FromQuery] string businessDate)
    {
        var response = await _mediator.Send(
            new AdmisiRajalPatientContextGetQuery(kind, id, businessDate));
        return Ok(new JSendOk(response));
    }
}
