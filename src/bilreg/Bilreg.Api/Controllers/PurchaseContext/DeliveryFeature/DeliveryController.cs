using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PurchaseContext.DeliveryFeature;

[Route("api/purchase/delivery")]
[ApiController]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeliveryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("draft")]
    public async Task<IActionResult> CreateDraft([FromBody] DeliveryCreateDraftCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPut("draft/{id}")]
    public async Task<IActionResult> UpdateDraft(string id, [FromBody] DeliveryUpdateDraftCommand cmd)
    {
        var effectiveCmd = cmd with { DeliveryOrderId = id };
        var response = await _mediator.Send(effectiveCmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] DeliveryConfirmCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("direct-confirm")]
    public async Task<IActionResult> DirectConfirm([FromBody] DeliveryDirectConfirmCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost("void")]
    public async Task<IActionResult> VoidDraft([FromBody] DeliveryVoidDraftCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new DeliveryGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    public async Task<IActionResult> ListByPeriode([FromQuery] string tglAwal, [FromQuery] string tglAkhir)
    {
        var query = new DeliveryListByPeriodeQuery(tglAwal, tglAkhir);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("pending-po")]
    public async Task<IActionResult> ListPendingPo()
    {
        var query = new DeliveryListPendingPoQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("validate-surat-jalan")]
    public async Task<IActionResult> ValidateSuratJalan(
        [FromQuery] string supplierId,
        [FromQuery] string suratJalanNo,
        [FromQuery] string? excludeDoId = null)
    {
        var query = new DeliveryValidateSuratJalanQuery(supplierId, suratJalanNo, excludeDoId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
