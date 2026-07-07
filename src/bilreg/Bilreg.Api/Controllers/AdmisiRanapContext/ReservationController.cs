using Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/reservation")]
[ApiController]
[Authorize]
public class ReservationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdmCreateReservationCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Maintain(string id, [FromBody] AdmMaintainReservationBody body)
    {
        var cmd = new AdmMaintainReservationCmd(
            id,
            body.PlannedDate,
            body.KelasId,
            body.BangsalId,
            body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new AdmGetReservationQry(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ReservationStatusEnum? status,
        [FromQuery] DateTime? plannedFrom,
        [FromQuery] DateTime? plannedTo)
    {
        var result = await _mediator.Send(new AdmListReservationQry(status, plannedFrom, plannedTo));
        return Ok(new JSendOk(result));
    }
}

public record AdmMaintainReservationBody(
    DateTime PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId);
