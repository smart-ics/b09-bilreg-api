using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.SalesContext.ResepFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ResepController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResepController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [Route("list/{regId}")]
    public async Task<IActionResult> ListDataByRegId(string regId)
    {
        var query = new ResepListByRegQuery(regId);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }
    
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new ResepGetQuery(id);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }
}