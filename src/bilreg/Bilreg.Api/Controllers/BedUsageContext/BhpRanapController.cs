using Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BhpRanapController : Controller
{
    private readonly IMediator _mediator;

    public BhpRanapController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{pakaiBedId}")]
    public async Task<IActionResult> AddBhp(string pakaiBedId, [FromBody] AddBhpRanapBody body)
    {
        var cmd = new BhpRanapAddCmd(
            PakaiBedId: pakaiBedId,
            BhpItemId: body.BhpItemId,
            BhpItemName: body.BhpItemName,
            Qty: body.Qty,
            Price: body.Price,
            UserId: body.UserId,
            RegId: body.RegId ?? string.Empty,
            LayananId: body.LayananId ?? string.Empty);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("batal")]
    [HttpPatch("batal")]
    public async Task<IActionResult> VoidBhp([FromBody] VoidBhpRanapBody body)
    {
        var cmd = new BhpRanapVoidCmd(
            BhpRanapId: body.BhpRanapId,
            UserId: body.UserId,
            VoidReason: body.VoidReason);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpDelete("{bhpRanapId}")]
    public async Task<IActionResult> DeleteBhp(string bhpRanapId, [FromQuery] string userId = "SPR001", [FromQuery] string voidReason = "Dibatalkan")
    {
        var cmd = new BhpRanapVoidCmd(
            BhpRanapId: bhpRanapId,
            UserId: string.IsNullOrWhiteSpace(userId) ? "SPR001" : userId,
            VoidReason: string.IsNullOrWhiteSpace(voidReason) ? "Dibatalkan" : voidReason);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
}

public record AddBhpRanapBody(
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    string UserId,
    string? RegId = null,
    string? LayananId = null);

public record VoidBhpRanapBody(
    string BhpRanapId,
    string UserId,
    string VoidReason);
