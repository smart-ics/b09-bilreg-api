using Bilreg.Application.SalesContext.PenjualanFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.SalesContext.PenjualanFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PenjualanController : ControllerBase
{
    private readonly IMediator _mediator;

    public PenjualanController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new PenjualanGetQry(id);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreatePenjualan(PenjualanCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
}