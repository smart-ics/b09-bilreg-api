using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub;

[Route("api/[controller]")]
[ApiController]
public class TindakanController : Controller
{
    private readonly IMediator _mediator;

    public TindakanController(IMediator mediator)
    {
        _mediator = mediator;
    }
    #region Order
    [HttpPost]
    [Route("order")]
    public async Task<IActionResult> CreateOrder(OrderTdkCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    [HttpPost]
    [Route("orderWithoutReg")]
    public async Task<IActionResult> CreateOrderWithoutReg(OrderTdkCreateWithoutRegCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("cancelOrderTdk")]
    public async Task<IActionResult> CancelOrder(OrderTdkCancelCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }


    [HttpGet]
    [Route("order/{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        var query = new OrderTdkGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("orderlist/{regId}/{layananId}")]
    public async Task<IActionResult> ListOrder(string regId, string layananId)
    {
        var query = new OrderTdkListQuery(regId, layananId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));

        

    }
    #endregion

    #region Tindakan

    [HttpPost]
    public async Task<IActionResult> CreateTindaka(TindakanCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("void")]
    public async Task<IActionResult> VoidTdk(TindakanVoidCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetTindakan(string id)
    {
        var query = new TindakanGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("list/{regId}")]
    public async Task<IActionResult> ListTindakan(string regId)
    {
        var query = new TindakanListQuery(regId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    
    #endregion
    
}
