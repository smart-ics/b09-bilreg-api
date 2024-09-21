﻿using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.DataSosialPasienSub;

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
    public async Task<IActionResult> Save(PasienCreateCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new PasienGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
    [HttpPut]
    [Route("SetContact")]
    public async Task<IActionResult> Save(PasienSetContactCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut]
    [Route("SetAlamat")]
    public async Task<IActionResult> SetAlamat(PasienSetAlamatCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    
    [HttpPut]
    [Route("SetKeluarga")]
    public async Task<IActionResult> SetKeluarga(PasienSetKeluargaCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
    
    [HttpGet]
    [Route("ByTglMedRec/{tglMedRec}")]
    public async Task<IActionResult> ListDataBytglMedRec(string tglMedRec)
    {
        var query = new PasienListQuery(tglMedRec);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
    [HttpGet]
    [Route("FindFastDataDuplicated/{id}")]
    public async Task<IActionResult> FindFastDataDuplicated(string id)
    {
        var query = new PasienFindFastDuplicated(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
    
    [HttpGet]
    [Route("FindThoroughDataDuplicated/{id}")]
    public async Task<IActionResult> FindThoroughDataDuplicated(string id)
    {
        var query = new PasienFindThoroughDuplicated(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}