using Bilreg.Application.BedUsageContext.RoomRateFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.RoomRateFeature;

[Route("api/[controller]")]
[ApiController]
public class RoomRateController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomRateController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    [Route("{kamarId}")]
    public async Task<IActionResult> GetRoomRate(string kamarId)
    {
        var query = new RoomRateGetQuery(kamarId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

}