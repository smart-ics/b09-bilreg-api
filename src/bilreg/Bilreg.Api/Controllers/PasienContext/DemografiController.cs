using Bilreg.Application.PasienContext.DemografiFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DemografiController : Controller
{
    private readonly IMediator _mediator;

    public DemografiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("negara/{id}")]
    public async Task<IActionResult> GetDataNegara(string id)
    {
        var query = new NegaraGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("negara")]
    public async Task<IActionResult> ListDataNegara()
    {
        var query = new NegaraListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("propinsi/{id}")]
    public async Task<IActionResult> GetDataPropinsi(string id)
    {
     var query = new PropinsiGetQuery(id);
     var response = await _mediator.Send(query);
     return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("propinsi")]
    public async Task<IActionResult> ListDataPropinsi()
    {
     var query = new PropinsiListQuery();
     var response = await _mediator.Send(query);
     return Ok(new JSendOk(response));
    }
    

    [HttpGet]
    [Route("kota/{id}")]
    public async Task<IActionResult> GetDataKota(string id)
    {
        var query = new KotaGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("kota")]
    public async Task<IActionResult> ListDataKota()
    {
        var query = new KotaListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("kabupaten/{id}")]
    public async Task<IActionResult> GetDataKabupaten(string id)
    {
        var query = new KabupatenGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("kabupaten/list/{propinsiId}")]
    public async Task<IActionResult> ListDataKabupaten(string propinsiId)
    {
        var query = new KabupatenListQuery(propinsiId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("kecamatan/{id}")]
    public async Task<IActionResult> GetDataKecamatan(string id)
    {
        var query = new KecamatanGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("kecamatan/list/{kabupatenId}")]
    public async Task<IActionResult> ListDataKecamatan(string kabupatenId)
    {
        var query = new KecamatanListQuery(kabupatenId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }


    [HttpGet]
    [Route("kelurahan/{id}")]
    public async Task<IActionResult> GetDataKelurahan(string id)
    {
        var query = new KecamatanGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("kelurahan/list/{keyword}")]
    public async Task<IActionResult> ListDataKelurahan(string keyword)
    {
        var query = new KelurahanListQuery(keyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}