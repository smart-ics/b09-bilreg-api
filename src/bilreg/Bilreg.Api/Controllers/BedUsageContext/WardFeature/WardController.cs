using Bilreg.Application.BedUsageContext.WardFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.WardFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WardController : ControllerBase
{
    private readonly IMediator _mediator;

    public WardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("KamarOk")]
    public async Task<IActionResult> ListKamarOk()
    {
        var query = new WardListKamarOkQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
