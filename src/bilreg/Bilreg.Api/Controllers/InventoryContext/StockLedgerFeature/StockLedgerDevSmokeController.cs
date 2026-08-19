using Bilreg.Api.Filters;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.InventoryContext.StockLedgerFeature;

/// <summary>
/// Development-only Stock Ledger smoke harness (ADR-STL-006 exception for ops/functional
/// testing against a snapshot DB). Thin MediatR wrappers — not a product API.
/// Requires <c>ASPNETCORE_ENVIRONMENT=Development</c> and
/// <c>StockLedger:AllowDevSmoke=true</c>.
/// </summary>
[Route("api/dev/stock-ledger-smoke")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(StockLedgerDevSmokeEnabledFilter))]
public sealed class StockLedgerDevSmokeController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockLedgerDevSmokeController(IMediator mediator) => _mediator = mediator;

    [HttpGet("reconcile")]
    public async Task<IActionResult> Reconcile(
        [FromQuery] string brgId,
        [FromQuery] string brgMasukReffId,
        [FromQuery] string? layananId = null)
    {
        var result = await _mediator.Send(new ReconcileScopeQuery(brgId, brgMasukReffId, layananId));
        return Ok(new JSendOk(result));
    }

    [HttpGet("availability")]
    public async Task<IActionResult> Availability(
        [FromQuery] string brgId,
        [FromQuery] string layananId,
        [FromQuery] DateTime? tglEd = null)
    {
        var result = await _mediator.Send(new GetAvailabilityAtLocationQuery(brgId, layananId, tglEd));
        return Ok(new JSendOk(result));
    }

    [HttpPost("goods-receipt")]
    public async Task<IActionResult> GoodsReceipt([FromBody] PostGoodsReceiptConsequenceCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] PostStockTransferConsequenceCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("sale-issue")]
    public async Task<IActionResult> SaleIssue([FromBody] PostSaleIssueConsequenceCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("sale-void")]
    public async Task<IActionResult> SaleVoid([FromBody] PostSaleVoidConsequenceCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("sales-return")]
    public async Task<IActionResult> SalesReturn([FromBody] PostSalesReturnConsequenceCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("internal-consumption")]
    public async Task<IActionResult> InternalConsumption(
        [FromBody] PostInternalConsumptionConsequenceCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
}
