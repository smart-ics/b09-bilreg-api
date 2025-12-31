using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/[controller]")]
[ApiController]
public class NilaiTarifController : Controller
{
    private readonly IMediator _mediator;

    public NilaiTarifController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("import")]
    public async Task<IActionResult> CreateByKtp(TrfImportNilaiTarifCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    [HttpPost]
    [Route("listTarifBrg")]
    public async Task<IActionResult> ListTarifBrg(TrfListTarifBrgQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}
