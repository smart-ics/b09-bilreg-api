using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Application.RadContext.RadOrderFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.RadContext;

[Route("api/rad/order")]
[Route("api/RadContext/RadOrderFeature")]
[ApiController]
[Authorize]
public class RadOrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public RadOrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [HttpPost("from-emr")]
    public async Task<IActionResult> CreateFromEmr([FromBody] RadOrderCreateFromEmrCmd cmd)
    {
        try
        {
            var response = await _mediator.Send(cmd);
            return Ok(new JSendOk(response));
        }
        catch (DuplicateRadOrderWarningException ex)
        {
            return Conflict(new JSendFail(new
            {
                code = DuplicateRadOrderWarningException.Code,
                message = ex.Message,
                ex.ExistingOrderNos
            }));
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelRadOrderRequest req)
    {
        await _mediator.Send(new RadOrderCancelCmd(id, req.UserId, req.Reason));
        return Ok(new JSendOk(new { message = "Order cancelled successfully." }));
    }

    [HttpPost("{id}/charge")]
    [HttpPatch("charge")]
    public async Task<IActionResult> Charge(string id, [FromBody] ChargeRadOrderRequest req)
    {
        var effectiveId = string.IsNullOrWhiteSpace(id) ? req.OrderId : id;
        var response = await _mediator.Send(new RadOrderChargeCmd(effectiveId, req.UserId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var response = await _mediator.Send(new RadOrderGetQuery(id));
        if (response is null)
            return NotFound(new JSendFail($"Order radiologi '{id}' tidak ditemukan."));

        return Ok(new JSendOk(response));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] DateTime? date,
        [FromQuery] string? modality,
        [FromQuery] int? status)
    {
        var response = await _mediator.Send(new RadOrderWorklistQuery(
            date ?? DateTime.Today,
            modality ?? string.Empty,
            status));
        return Ok(new JSendOk(response));
    }

    [HttpGet("/api/rad/tarif-modality")]
    [HttpGet("tarif-modality")]
    public async Task<IActionResult> TarifModalityLookup([FromQuery] string? modality = null)
    {
        var response = await _mediator.Send(new RadTarifModalityLookupQuery(modality));
        return Ok(new JSendOk(response));
    }
}

public record CancelRadOrderRequest(string UserId, string Reason);
public record ChargeRadOrderRequest(string UserId, string OrderId = "");

public class JSendFail
{
    public string status { get; set; } = "fail";
    public object? data { get; set; }

    public JSendFail(object? data = null)
    {
        status = "fail";
        this.data = data;
    }
}
