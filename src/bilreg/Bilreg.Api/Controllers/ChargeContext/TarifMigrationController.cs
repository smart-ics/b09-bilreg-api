using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/tarif-migration")]
[ApiController]
[Authorize]
public class TarifMigrationController : Controller
{
    private readonly IMediator _mediator;

    public TarifMigrationController(IMediator mediator) => _mediator = mediator;

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _mediator.Send(new TrfGetTarifMigrationStatusQry());
        return Ok(new JSendOk(result));
    }

    [HttpGet("consistency")]
    public async Task<IActionResult> GetConsistency()
    {
        var result = await _mediator.Send(new TrfCheckTarifProjectionConsistencyQry());
        return Ok(new JSendOk(result));
    }

    [HttpPost("baseline")]
    public async Task<IActionResult> CreateBaseline([FromBody] TrfCreateBaselineTarifPolicyBody body)
    {
        var cmd = new TrfCreateBaselineTarifPolicyCmd(
            body.UserId,
            body.PolicyNo,
            body.PublishedBy ?? body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("mode")]
    public async Task<IActionResult> SetMode([FromBody] TrfSetTarifMigrationModeBody body)
    {
        await _mediator.Send(new TrfSetTarifMigrationModeCmd(body.Mode, body.UserId));
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete("mode")]
    public async Task<IActionResult> ClearMode([FromBody] TrfClearTarifMigrationModeBody body)
    {
        await _mediator.Send(new TrfSetTarifMigrationModeCmd(null, body.UserId));
        return Ok(new JSendOk("Done"));
    }
}

public record TrfCreateBaselineTarifPolicyBody(
    string UserId,
    string? PolicyNo = null,
    string? PublishedBy = null);

public record TrfSetTarifMigrationModeBody(
    TarifMigrationMode? Mode,
    string UserId);

public record TrfClearTarifMigrationModeBody(string UserId);
