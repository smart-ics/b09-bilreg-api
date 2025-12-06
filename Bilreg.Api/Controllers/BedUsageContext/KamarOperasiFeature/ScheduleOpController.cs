using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
public class ScheduleOpController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScheduleOpController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{tgl}/TglOp")]
    public async Task<IActionResult> ListSchedule(string tgl)
    {
        var query = new OkListScheduleQuery(tgl);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}