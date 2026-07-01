using Farinv.Application.InventoryContext.MutasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.InventoryContext.MutasiFeature;

[Route("api/[controller]")]
[ApiController]
public class OrderMutasiController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderMutasiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("addItem")]
    public async Task<IActionResult> AddItem(OrderMutasiAddItemCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("submit")]
    public async Task<IActionResult> Submit(OrderMutasiSubmitCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("approve")]
    public async Task<IActionResult> Approve(OrderMutasiApproveCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("reject")]
    public async Task<IActionResult> Reject(OrderMutasiRejectCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        var query = new OrderMutasiGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("layananOrder/{id}/draft")]
    public async Task<IActionResult> GetOrderDraft(string id)
    {
        var query = new OrderMutasiDraftStateGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("submitted/{tglYmd1}/{tglYmd2}")]
    public async Task<IActionResult> ListSubmitted(string tglYmd1, string tglYmd2)
    {
        var query = new OrderMutasiSubmittedListQuery(tglYmd1, tglYmd2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("approved/{tglYmd1}/{tglYmd2}")]
    public async Task<IActionResult> ListApproved(string tglYmd1, string tglYmd2)
    {
        var query = new OrderMutasiApprovedListQuery(tglYmd1, tglYmd2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
