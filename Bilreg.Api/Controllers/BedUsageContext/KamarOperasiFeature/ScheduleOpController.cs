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

    [HttpPost]
    [Route("SetSchedule/{orderOpId}/{kamarId}/{tgl}/{jam}/{userId}")]
    public async Task<IActionResult> SetSchedule(string orderOpId, string kamarId,
        string tgl, string jam, string userId)
    {
        var cmd = new OkScheduleOpSetCommand(orderOpId, kamarId, tgl, jam, userId);
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("AddPpa/{orderOpId}/{ppaId}/{userId}")]
    public async Task<IActionResult> AddPpa(string orderOpId, string ppaId, string userId)
    {
        var cmd = new OkScheduleOpAddPpaCommand(orderOpId, ppaId, userId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete]
    [Route("RemovePpa/{orderOpId}/{ppaId}/{userId}")]
    public async Task<IActionResult> RemovePpa(string orderOpId, string ppaId, string userId)
    {
        var cmd = new OkScheduleOpRemovePpaCommand(orderOpId, ppaId, userId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("AssignLeader/{orderOpId}/{ppaId}/{userId}")]
    public async Task<IActionResult> AssignLeader(string orderOpId, string ppaId, string userId)
    {
        var cmd = new OkScheduleOpAssignLeaderCommand(orderOpId, ppaId, userId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}