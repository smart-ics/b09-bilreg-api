using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.InventoryContext.MutasiFeature;

[Route("api/[controller]")]
[ApiController]
public class TerimaMutasiController : ControllerBase
{
    private readonly IMediator _mediator;

    public TerimaMutasiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("confirm")]
    public async Task<IActionResult> Confirm(TerimaMutasiConfirmCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new TerimaMutasiGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("by-mutasi/{mutasiBrgId}")]
    public async Task<IActionResult> GetByMutasi(string mutasiBrgId)
    {
        var query = new TerimaMutasiGetByMutasiQuery(mutasiBrgId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list/{tgl1}/{tgl2}")]
    public async Task<IActionResult> List(string tgl1, string tgl2)
    {
        var query = new TerimaMutasiListQuery(tgl1, tgl2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list/{tgl1}/{tgl2}/tujuan/{layananTujuanId}")]
    public async Task<IActionResult> ListByTujuan(string tgl1, string tgl2, string layananTujuanId)
    {
        var query = new TerimaMutasiListQuery(tgl1, tgl2, layananTujuanId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
