using Bilreg.Application.BedUsageContext.RoomChargeFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.RoomChargeFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RoomChargeController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomChargeController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetRoomCharge(string id)
    {
        var query = new RoomChargeGetQry(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{regId}/register")]
    public async Task<IActionResult> ListByReg(string regId)
    {
        var query = new RoomChargeListByRegQry(regId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{pakaiBedId}/pakaiBed")]
    public async Task<IActionResult> ListByPakaiBed(string pakaiBedId)
    {
        var query = new RoomChargeListByPakaiBedQry(pakaiBedId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
