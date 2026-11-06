using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/opname-request")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
public class OpnameRequestController : ControllerBase
{
    private readonly IMediator _mediator;

    public OpnameRequestController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdmCreateOpnameRequestPostModel body)
    {
        var extRef = !string.IsNullOrWhiteSpace(body.ExternalReferenceId)
            ? body.ExternalReferenceId
            : (!string.IsNullOrWhiteSpace(body.TrsOrderId) ? body.TrsOrderId : "-");

        var orderSource = body.OrderSource ?? OrderSourceEnum.EmrRawatJalan;
        var extSource = !string.IsNullOrWhiteSpace(body.ExternalSystemSource)
            ? body.ExternalSystemSource
            : (orderSource == OrderSourceEnum.EmrIgd ? "EMR_IGD" : "EMR_RJ");

        var cmd = new AdmCreateOpnameRequestCmd(
            body.PasienId,
            body.DokterId,
            body.PlannedDate,
            body.ClinicalNotes ?? "",
            extRef,
            body.UserId,
            extSource,
            orderSource);

        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] AdmCancelOpnameRequestBody body)
    {
        await _mediator.Send(new AdmCancelInpatientOrderCmd(id, body.UserId, body.Reason));
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new AdmGetOpnameRequestQry(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] OpnameRequestStatusEnum? status)
    {
        var result = await _mediator.Send(new AdmListOpnameRequestQry(status));
        return Ok(new JSendOk(result));
    }

    [HttpPatch("{id}/setInsurance")]
    [HttpPut("{id}/insurance")]
    public async Task<IActionResult> SetInsurance(string id, [FromBody] AdmSetInsuranceRequestBody body)
    {
        await _mediator.Send(new AdmSetInpatientOrderInsuranceCmd(id, body.TipeJaminanId, body.ReffId, body.UserId));
        return Ok(new JSendOk("Done"));
    }
    
    [HttpGet("trs-order/{trsOrderId}")]
    public async Task<IActionResult> GetByEmrOrder(string trsOrderId)
    {
        var result = await _mediator.Send(new AdmGetOpnameRequestByTrsOrderIdQry(trsOrderId));
        return Ok(new JSendOk(result));
    }
}

public record AdmCreateOpnameRequestPostModel(
    string PasienId,
    string DokterId,
    string PlannedDate,
    string ClinicalNotes,
    string UserId,
    string? ExternalReferenceId = null,
    string? ExternalSystemSource = null,
    string? TrsOrderId = null,
    OrderSourceEnum? OrderSource = null);

public record AdmCancelOpnameRequestBody(string UserId, string Reason);

public record AdmSetInsuranceRequestBody(string TipeJaminanId, string ReffId, string UserId);
