using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
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
    public async Task<IActionResult> Create(PasienCreateCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut]
    public async Task<IActionResult> Update(PasienUpdateCommand cmd)
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
        
    [HttpGet]
    [Route("find/{tglLahirYmd}/{pasienName}")]
    public async Task<IActionResult> GetData(string tglLahirYmd, string pasienName)
    {
        var query = new PasienFindFast(tglLahirYmd, pasienName);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

}