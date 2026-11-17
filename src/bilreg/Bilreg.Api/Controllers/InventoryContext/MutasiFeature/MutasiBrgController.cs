using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.InventoryContext.MutasiFeature;

[Route("api/[controller]")]
[ApiController]
public class MutasiBrgController : ControllerBase
{
    private readonly IMediator _mediator;

    public MutasiBrgController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("draft")]
    public async Task<IActionResult> CreateDraft(MutasiBrgCreateDraftCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("addItem")]
    public async Task<IActionResult> AddItem(MutasiBrgAddItemCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("removeItem")]
    public async Task<IActionResult> RemoveItem(MutasiBrgRemoveItemCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("dispatch")]
    public async Task<IActionResult> Dispatch(MutasiBrgDispatchCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("cancel")]
    public async Task<IActionResult> Cancel(MutasiBrgCancelCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new MutasiBrgGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list/{tgl1}/{tgl2}")]
    public async Task<IActionResult> List(string tgl1, string tgl2)
    {
        var query = new MutasiBrgListQuery(tgl1, tgl2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("in-transit/tujuan/{layananTujuanId}")]
    public async Task<IActionResult> ListInTransit(string layananTujuanId)
    {
        var query = new MutasiBrgInTransitListQuery(layananTujuanId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
