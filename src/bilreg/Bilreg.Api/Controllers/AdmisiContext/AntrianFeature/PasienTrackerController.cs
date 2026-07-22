using Bilreg.Application.AdmisiContext.AntrianFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PasienTrackerController : ControllerBase
{
    private readonly IMediator _mediator;

    public PasienTrackerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{pasienTrackerId}")]
    public async Task<IActionResult> Get(string pasienTrackerId)
    {
        var query = new TrkGetQuery(pasienTrackerId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("candidates")]
    public async Task<IActionResult> ListCandidates(
        [FromQuery] string personName,
        [FromQuery] string tglLahir,
        [FromQuery] string relevantDate)
    {
        var query = new TrkJourneyCandidateListQry(personName, tglLahir, relevantDate);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("resolve/select")]
    public async Task<IActionResult> ResolveSelect(TrkJourneyResolveSelectCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("pharmacy/evidence")]
    public async Task<IActionResult> AppendPharmacyEvidence(TrkAppendPharmacyEvidenceCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}
