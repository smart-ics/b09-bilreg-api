using System.Security.Claims;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderOpController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderOpController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("CreateByReg")]
    public async Task<IActionResult> CreateOrderByReg(OkCreateOrderOpByRegCmd cmd)
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            cmd = cmd with { UserId = userId };
        }
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("Cancel")]
    public async Task<IActionResult> Cancel(OkCancelOrderOpCommand cmd)
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            cmd = cmd with { UserId = userId };
        }
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    public async Task<IActionResult> ListOrder(
        [FromQuery] string? status = null,
        [FromQuery] string? tglAwal = null,
        [FromQuery] string? tglAkhir = null,
        [FromQuery] int? limit = null)
    {
        var query = new OkListOperasiAktifQuery(status, tglAwal, tglAkhir, limit);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("Discharge")]
    public async Task<IActionResult> DischargeOp(OkDischargeOpCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    private string GetUserId()
    {
        var user = HttpContext?.User;
        if (user == null)
            return string.Empty;

        return user.FindFirstValue("UserId")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? string.Empty;
    }
}

public record OrderOpCancelRequest(string OrderOpId, string AlasanBatal)
{
    public static implicit operator OkCancelOrderOpCommand(OrderOpCancelRequest req) =>
        new(req.OrderOpId, req.AlasanBatal);
}
