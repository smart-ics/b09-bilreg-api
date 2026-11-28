using Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.InventoryContext.LogistikMedisFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LogistikMedisController : ControllerBase
{
    private readonly IMediator _mediator;

    public LogistikMedisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stok-kanvas/{layananId}")]
    public async Task<IActionResult> StokKanvas(string layananId, [FromQuery] string? keyword = null)
    {
        var query = new LogistikMedisStokKanvasQuery(layananId, keyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("order-mutasi/{layananOrderId}/{tglYmd1}/{tglYmd2}")]
    public async Task<IActionResult> ListOrderMutasi(string layananOrderId, string tglYmd1, string tglYmd2)
    {
        var query = new LogistikMedisOrderMutasiListQuery(layananOrderId, tglYmd1, tglYmd2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost("order-mutasi/submit-batch")]
    public async Task<IActionResult> SubmitOrderMutasiBatch([FromBody] LogistikMedisOrderMutasiSubmitBatchCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("mutasi-masuk/{layananTujuanId}/{tglYmd1}/{tglYmd2}")]
    public async Task<IActionResult> ListMutasiMasuk(string layananTujuanId, string tglYmd1, string tglYmd2)
    {
        var query = new LogistikMedisMutasiMasukQuery(layananTujuanId, tglYmd1, tglYmd2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("mutasi-keluar/{layananAsalId}/{tglYmd1}/{tglYmd2}")]
    public async Task<IActionResult> ListMutasiKeluar(string layananAsalId, string tglYmd1, string tglYmd2)
    {
        var query = new LogistikMedisMutasiKeluarListQuery(layananAsalId, tglYmd1, tglYmd2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpPost("mutasi-keluar/dispatch-batch")]
    public async Task<IActionResult> DispatchMutasiKeluarBatch([FromBody] LogistikMedisMutasiKeluarDispatchBatchCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}
