using Bilreg.Application.PasienContext.StatusSosialFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.StatusSosialSub;

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
}