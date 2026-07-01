using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/tarif-policy")]
[ApiController]
[Authorize]
public class TarifPolicyController : Controller
{
    private readonly IMediator _mediator;

    public TarifPolicyController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TrfCreateTarifPolicyCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new TrfGetTarifPolicyQry(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] TarifPolicyStatus? policyStatus,
        [FromQuery] string keyword = "")
    {
        var result = await _mediator.Send(new TrfListTarifPolicyQry(policyStatus, keyword));
        return Ok(new JSendOk(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TrfUpdateTarifPolicyBody body)
    {
        var cmd = new TrfUpdateTarifPolicyCmd(
            id,
            body.PolicyNo,
            body.PolicyName,
            body.EffectiveDateInfo,
            body.Description,
            body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/variant")]
    public async Task<IActionResult> AddVariant(string id, [FromBody] TrfAddTarifPolicyVariantBody body)
    {
        var cmd = new TrfAddTarifPolicyVariantCmd(
            id,
            body.TarifId,
            body.KelasId,
            body.TipeTarifId,
            body.Komponen,
            body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut("{id}/variant/{itemNo:int}")]
    public async Task<IActionResult> UpdateVariant(
        string id,
        int itemNo,
        [FromBody] TrfUpdateTarifPolicyVariantBody body)
    {
        var cmd = new TrfUpdateTarifPolicyVariantCmd(
            id,
            itemNo,
            body.TarifId,
            body.KelasId,
            body.TipeTarifId,
            body.Komponen,
            body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete("{id}/variant/{itemNo:int}")]
    public async Task<IActionResult> RemoveVariant(string id, int itemNo)
    {
        await _mediator.Send(new TrfRemoveTarifPolicyVariantCmd(id, itemNo));
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/copy")]
    public async Task<IActionResult> Copy(string id, [FromBody] TrfCopyTarifPolicyBody body)
    {
        var cmd = new TrfCopyTarifPolicyCmd(id, body.NewPolicyNo, body.NewPolicyName, body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/mass-adjustment")]
    public async Task<IActionResult> MassAdjustment(string id, [FromBody] TrfMassAdjustTarifPolicyBody body)
    {
        var cmd = new TrfMassAdjustTarifPolicyCmd(
            id,
            body.Scope,
            body.AdjustmentType,
            body.Value,
            body.UserId);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> Review(string id, [FromBody] TrfReviewTarifPolicyBody body)
    {
        var result = await _mediator.Send(new TrfReviewTarifPolicyCmd(id, body.UserId));
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> Publish(string id, [FromBody] TrfPublishTarifPolicyBody body)
    {
        var cmd = new TrfPublishTarifPolicyCmd(id, body.PublishedBy, body.Note ?? "");
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet("{id}/publish-log")]
    public async Task<IActionResult> ListPublishLog(string id)
    {
        var result = await _mediator.Send(new TrfListTarifPolicyPublishLogQry(id));
        return Ok(new JSendOk(result));
    }
}

public record TrfUpdateTarifPolicyBody(
    string PolicyNo,
    string PolicyName,
    DateTime EffectiveDateInfo,
    string Description,
    string UserId);

public record TrfAddTarifPolicyVariantBody(
    string TarifId,
    string KelasId,
    string TipeTarifId,
    IReadOnlyList<TrfVariantKomponenInput> Komponen,
    string UserId);

public record TrfUpdateTarifPolicyVariantBody(
    string TarifId,
    string KelasId,
    string TipeTarifId,
    IReadOnlyList<TrfVariantKomponenInput> Komponen,
    string UserId);

public record TrfCopyTarifPolicyBody(string NewPolicyNo, string NewPolicyName, string UserId);

public record TrfMassAdjustTarifPolicyBody(
    string Scope,
    string AdjustmentType,
    decimal Value,
    string UserId);

public record TrfReviewTarifPolicyBody(string UserId);

public record TrfPublishTarifPolicyBody(string PublishedBy, string? Note);
