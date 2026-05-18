using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.LabContext.LabOrderFeature;

[Route("api/LabContext/LabOrderFeature")]
[ApiController]
public class LabOrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public LabOrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("fromEmr")]
    public async Task<IActionResult> CreateFromEmr(LabOrderCreateFromEmrCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("external")]
    public async Task<IActionResult> CreateExternal(LabOrderCreateExternalPatientCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> Worklist(
        [FromQuery] int? labOrderStatus,
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2)
    {
        var response = await _mediator.Send(new LabOrderWorklistQuery(
            labOrderStatus, searchTerm, date1, date2));
        return Ok(new JSendOk(response));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId)
    {
        var response = await _mediator.Send(new LabOrderGetQuery(orderId));
        return Ok(new JSendOk(response));
    }
}
