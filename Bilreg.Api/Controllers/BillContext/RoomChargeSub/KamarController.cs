using Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RoomChargeSub;

[Route("api/[controller]")]
[ApiController]
public class KamarController : ControllerBase
{
    private readonly IMediator _mediator;

    public KamarController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(KamarSaveCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var cmd = new KamarDeleteCommand(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new KamarGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
    [HttpGet]
    [Route("ByIdBangsal/{idBangsal}")]
    public async Task<IActionResult> ListDataByIdBangsal(string idBangsal)
    {
        var query = new KamarListDataByBangsalId(idBangsal);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
    [HttpGet]
    [Route("ByIdKelas/{idKelas}")]
    public async Task<IActionResult> ListDataByIdKelas(string idKelas)
    {
        var query = new KamarListDataByKelasId(idKelas);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
}