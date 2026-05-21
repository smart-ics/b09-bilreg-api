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

    [HttpGet("releaseWorklist")]
    public async Task<IActionResult> ReleaseWorklist(
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? date1,
        [FromQuery] DateTime? date2)
    {
        var response = await _mediator.Send(new LabOrderReleaseWorklistQuery(searchTerm, date1, date2));
        return Ok(new JSendOk(response));
    }

    [HttpGet("collectionPreparation")]
    public async Task<IActionResult> CollectionPreparation([FromQuery] string orderId)
    {
        var response = await _mediator.Send(new LabCollectionPreparationQuery(orderId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("byEmrOrderId/{emrOrderId}")]
    public async Task<IActionResult> GetByEmrOrderId(string emrOrderId)
    {
        var response = await _mediator.Send(new LabOrderGetByEmrOrderIdQuery(emrOrderId));
        return Ok(new JSendOk(response));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Get(string orderId)
    {
        var response = await _mediator.Send(new LabOrderGetQuery(orderId));
        return Ok(new JSendOk(response));
    }

    [HttpPatch("defer")]
    public async Task<IActionResult> Defer(LabOrderDeferCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("activateDeferred")]
    public async Task<IActionResult> ActivateDeferred(LabOrderActivateDeferredCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch("charge")]
    public async Task<IActionResult> Charge(LabOrderChargeCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch("collect")]
    public async Task<IActionResult> Collect(LabOrderCollectSpecimenCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("approveFinancialClearance")]
    public async Task<IActionResult> ApproveFinancialClearance(LabOrderApproveFinancialClearanceCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("rejectFinancialClearance")]
    public async Task<IActionResult> RejectFinancialClearance(LabOrderRejectFinancialClearanceCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("release")]
    public async Task<IActionResult> Release(LabOrderReleaseCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("cancel")]
    public async Task<IActionResult> Cancel(LabOrderCancelCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch("terminate")]
    public async Task<IActionResult> Terminate(LabOrderTerminateCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}
