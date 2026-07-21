using Bilreg.Api.Filters;
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
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
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
        [FromQuery] string? plannedFrom,
        [FromQuery] string? plannedTo)
    {
        var result = await _mediator.Send(new AdmListReservationQry(status, plannedFrom, plannedTo));
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cansel(string id, [FromBody] AdmCancelReservationBody body)
    {
        await _mediator.Send(new AdmCancelReservationCmd(id, body.UserId));
        return Ok(new JSendOk("Done"));
    }
}

public record AdmMaintainReservationBody(
    string PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId);

public record AdmCancelReservationBody(string UserId);