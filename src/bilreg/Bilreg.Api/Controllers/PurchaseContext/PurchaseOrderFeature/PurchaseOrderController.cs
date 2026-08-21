using Bilreg.Application.PurchaseContext.PurchaseOrderFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PurchaseContext.PurchaseOrderFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PurchaseOrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchaseOrderController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [Route("list/{tglAwalYmd}/{tglAkhirYmd}")]
    public async Task<IActionResult> ListDataByPeriode(string tglAwalYmd, string tglAkhirYmd)
    {
        var query = new PurchaseOrderListByPeriodeQuery(tglAwalYmd, tglAkhirYmd);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }
    
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new PurchaseOrderGetQuery(id);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }
}
