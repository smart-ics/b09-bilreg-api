using Bilreg.Application.PasienContext.StatusSosialFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext;

[Route("api/[controller]")]
[ApiController]
public class StatusSosialController : ControllerBase
{
    private readonly IMediator _mediator;

    public StatusSosialController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("agama/{id}")]
    public async Task<IActionResult> GetDataAgama(string id)
    {
        var result = await _mediator.Send(new AgamaGetQuery(id));
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("agama")]
    public async Task<IActionResult> ListDataAgama()
    {
        var result = await _mediator.Send(new AgamaListQuery());
        return Ok(new JSendOk(result));
    }    

    
    [HttpGet]
    [Route("pekerjaanDk/{id}")]
    public async Task<IActionResult> GetDataPekerjaanDk(string id)
    {
        var result = await _mediator.Send(new PekerjaanDkGetQuery(id));
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("pekerjaanDk")]
    public async Task<IActionResult> ListDataPekerjaanDk()
    {
        var result = await _mediator.Send(new PekerjaanDkListQuery());
        return Ok(new JSendOk(result));
    }    

    
    [HttpGet]
    [Route("pendidikanDk/{id}")]
    public async Task<IActionResult> GetDataPendidikanDk(string id)
    {
        var result = await _mediator.Send(new PendidikanDkGetQuery(id));
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("pendidikanDk")]
    public async Task<IActionResult> ListDataPendidikanDk()
    {
        var result = await _mediator.Send(new PendidikanDkListQuery());
        return Ok(new JSendOk(result));
    }

    
    [HttpGet]
    [Route("statusKawinDk/{id}")]
    public async Task<IActionResult> GetDataStatusKawinDk(string id)
    {
        var result = await _mediator.Send(new StatusKawinDkGetQuery(id));
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("statusKawinDk")]
    public async Task<IActionResult> ListDataStatusKawinDk()
    {
        var result = await _mediator.Send(new StatusKawinDkListQuery());
        return Ok(new JSendOk(result));
    }

    
    [HttpGet]
    [Route("suku/{id}")]
    public async Task<IActionResult> GetDataSuku(string id)
    {
        var result = await _mediator.Send(new SukuGetQuery(id));
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("suku")]
    public async Task<IActionResult> ListDataSuku()
    {
        var result = await _mediator.Send(new SukuListQuery());
        return Ok(new JSendOk(result));
    }
}