using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TransportSub;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TujuanTransportController : ControllerBase
{
    private readonly IMediator _mediator;

    public TujuanTransportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] TujuanTransportSaveCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("SetDefaultAmbulance")]
    public async Task<IActionResult> SetDefaultAmbulance([FromBody] TujuanTransportSetDefaultAmbulanceCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut("UnSetDefaultAmbulance/{id}")]
    public async Task<IActionResult> UnSetDefaultAmbulance(string id)
    {
        var cmd = new TujuanTransportUnSetDefaultAmbulanceCommand(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var cmd = new TujuanTransportDeleteCommand(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new TujuanTransportGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new TujuanTransportListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
