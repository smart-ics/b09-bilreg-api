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
    [Route("SetSchedule")]
    public async Task<IActionResult> SetSchedule(OkScheduleOpSetCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("AddPpa")]
    public async Task<IActionResult> AddPpa(OkScheduleOpAddPpaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("RemovePpa")]
    public async Task<IActionResult> RemovePpa(OkScheduleOpRemovePpaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("AssignLeader")]
    public async Task<IActionResult> AssignLeader(OkScheduleOpAssignLeaderCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("Cancel")]
    public async Task<IActionResult> CancelSchedule(OkScheduleOpCancelCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{orderOpId}")]
    public async Task<IActionResult> GetSchedule(string orderOpId)
    {
        var query = new OkScheduleOpGetQuery(orderOpId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}