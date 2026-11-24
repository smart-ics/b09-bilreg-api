using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TransportSub;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AmbulanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public AmbulanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] AmbulanceSaveCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("AddKomponen")]
    public async Task<IActionResult> AddKomponen([FromBody] AmbulanceAddKomponenCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("RemoveKomponen")]
    public async Task<IActionResult> RemoveKomponen([FromBody] AmbulanceRemoveKomponenCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("AddTujuan")]
    public async Task<IActionResult> AddTujuan([FromBody] AmbulanceAddTujuanCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("RemoveTujuan")]
    public async Task<IActionResult> RemoveTujuan([FromBody] AmbulanceRemoveTujuanCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut("Activate/{id}")]
    public async Task<IActionResult> Activate(string id)
    {
        var cmd = new AmbulanceActivateCommand(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut("Deactivate/{id}")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var cmd = new AmbulanceDeactivateCommand(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new AmbulanceGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new AmbulanceListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
