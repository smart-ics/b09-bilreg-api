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
        [FromQuery] int limit = 100) =>
        Ok(new JSendOk(await _mediator.Send(new AdmisiRajalOfficerWorklistQuery(
            businessDate, servicePointId, queueStatus, loketKey, offset, limit))));

    [HttpGet("patient-context-search")]
    public async Task<IActionResult> PatientContextSearch([FromQuery] string keyword, [FromQuery] string businessDate,
        [FromQuery] int limitPerType = 10) =>
        Ok(new JSendOk(await _mediator.Send(new AdmisiRajalPatientContextSearchQuery(keyword, businessDate, limitPerType))));
}
