using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

/// <summary>
/// Admisi Rajal officer read composition. Queue membership and state remain owned by
/// Patient Tracker <c>/api/v1/admission-queue/worklist</c>; this route does not write queue truth.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/admisi-rajal")]
public sealed class AdmisiRajalOfficerWorklistController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdmisiRajalOfficerWorklistController(IMediator mediator) => _mediator = mediator;

    [HttpGet("officer-worklist")]
    public async Task<IActionResult> OfficerWorklist(
        [FromQuery] string businessDate,
        [FromQuery] string? servicePointId,
        [FromQuery] int? queueStatus,
        [FromQuery] string? loketKey,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100,
        [FromQuery] bool activeOnly = false,
        [FromQuery] bool includePagingMetadata = false)
    {
        var response = await _mediator.Send(new AdmisiRajalOfficerWorklistQuery(
            businessDate, servicePointId, queueStatus, loketKey, offset, limit, activeOnly));
        object data = includePagingMetadata ? response : response.Items;
        return Ok(new JSendOk(data));
    }
}
