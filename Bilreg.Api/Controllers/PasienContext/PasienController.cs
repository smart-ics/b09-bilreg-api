using Bilreg.Application.PasienContext.PasienFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext;

[Route("api/[controller]")]
[ApiController]
public class PasienController : Controller
{
    private readonly IMediator _mediator;

    public PasienController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(PasienCreateCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("createByKtp")]
    public async Task<IActionResult> CreateByKtp(PasienCreateByKtpCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new PasienGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }


    [HttpGet]
    [Route("search/{keyword}")]
    public async Task<IActionResult> Search(string keyword)
    {
        var query = new PasienSearchQuery(keyword);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("ktp")]
    public async Task<IActionResult> SetDataKpt(PasienSetKtpCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("addContact")]
    public async Task<IActionResult> AddContact(PasienAddContactCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("demografi")]
    public async Task<IActionResult> SetStatusSosial(PasienSetDemografiCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("nonActive")]
    public async Task<IActionResult> NonActive(PasienNonActiveCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("reActive")]
    public async Task<IActionResult> ReActive(PasienReActiveCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

}