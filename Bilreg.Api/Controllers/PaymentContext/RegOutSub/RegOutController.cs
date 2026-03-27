using Bilreg.Application.PaymentContext.RegOutFeature.UseCase;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PaymentContext.RegOutSub;

[Route("api/[controller]")]
[ApiController]
public class RegOutController : Controller
{
    private readonly IMediator _mediator;
    public RegOutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("listRegDischargeable")]
    public async Task<IActionResult> ListRegDischargeable()
    {
        var query = new RegListRegDischargeableQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet]
    [Route("search/{keyword}")]
    public async Task<IActionResult> SearchReg(string keyword)
    {
        var query = new RegSearchRegQuery(keyword);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new RegGetRegQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet]
    [Route("listSummaryBill/{id}")]
    public async Task<IActionResult> ListSummaryBill(string id)
    {
        var query = new TrsbListSummaryBillQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet]
    [Route("listAlokasiPembayaran/{id}")]
    public async Task<IActionResult> ListAlokasiPembayaran(string id)
    {
        var query = new RegListAlokasiPembayaranQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [Route("addPembayaran/")]
    public async Task<IActionResult> AddPembayaran(RegAddPembayaranCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("updatePembayaran")]
    public async Task<IActionResult> UpdatePembayaran(RegUpdateNilaiBayarCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}
