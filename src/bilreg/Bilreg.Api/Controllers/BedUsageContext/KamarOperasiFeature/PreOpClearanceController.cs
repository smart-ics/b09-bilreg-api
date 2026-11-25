using System.Security.Claims;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PreOpClearanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreOpClearanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Save")]
    public async Task<IActionResult> Save(
        OkPreOpClearanceSaveCommand cmd,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                cmd = cmd with { UserId = userId };
            }

            var response = await _mediator.Send(cmd, cancellationToken);
            return Ok(new JSendOk(response));
        }
        catch (PreOpChecklistIncompleteException ex)
        {
            return UnprocessableEntity(new JSendFail(ex.Message));
        }
    }

    [HttpGet("Get")]
    public async Task<IActionResult> Get(
        [FromQuery] string? orderOpId = null,
        [FromQuery] string? scheduleOpId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new OkPreOpClearanceGetQuery(orderOpId, scheduleOpId);
        var response = await _mediator.Send(query, cancellationToken);
        return Ok(new JSendOk(response));
    }

    private string GetUserId()
    {
        var user = HttpContext?.User;
        if (user == null)
            return string.Empty;

        return user.FindFirstValue("UserId")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("pegId")
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? string.Empty;
    }
}
