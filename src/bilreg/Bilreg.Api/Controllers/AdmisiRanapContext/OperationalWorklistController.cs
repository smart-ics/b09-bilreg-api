using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/operational-worklist")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
public class OperationalWorklistController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperationalWorklistController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? jenis,
        [FromQuery] string? dokterId,
        [FromQuery] string? bangsalId,
        [FromQuery] string? kelasId,
        [FromQuery] string? tipeJaminanId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? searchTerm,
        [FromQuery] bool includeTerminal = false)
    {
        var result = await _mediator.Send(new AdmListOperationalWorklistQry(
            jenis,
            dokterId,
            bangsalId,
            kelasId,
            tipeJaminanId,
            dateFrom,
            dateTo,
            searchTerm,
            includeTerminal));

        return Ok(new JSendOk(result));
    }
}
