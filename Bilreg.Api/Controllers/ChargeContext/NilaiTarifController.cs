using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NilaiTarifController : Controller
{
    private readonly IMediator _mediator;

    public NilaiTarifController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("import")]
    public async Task<IActionResult> Import([FromBody] TrfImportNilaiTarifBody? body = null)
    {
        var cmd = new TrfImportNilaiTarifCmd(
            body?.ImportedBy,
            body?.IsEmergency ?? false);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("tarif-brg")]
    public async Task<IActionResult> ListTarifBrg(        
        [FromQuery] string layananId,
        [FromQuery] string kelasId,
        [FromQuery] string tipeTarifId,
        [FromQuery] string keyword)
    {
        var query = new TrfListTarifBrgQuery(layananId, kelasId, tipeTarifId, keyword);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    //[HttpGet]
    //[Route("{tarifId}/{kelasId}/{tipeTarifId}")]
    //public async Task<IActionResult> ListTarifBrg(string tarifId,
    //    string kelasId, string tipeTarifId)
    //{
    //    var query = new TrfGetNilaiTarifQuery(tarifId, kelasId, tipeTarifId);
    //    var result = await _mediator.Send(query);
    //    return Ok(new JSendOk(result));
    //}

}

public record TrfImportNilaiTarifBody(string? ImportedBy = null, bool IsEmergency = false);
