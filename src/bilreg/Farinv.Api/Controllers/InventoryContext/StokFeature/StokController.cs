using Farinv.Application.InventoryContext.StokFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.InventoryContext.StokFeature;
[Route("api/[controller]")]
[ApiController]
public class StokController : ControllerBase
{
    private readonly IMediator _mediator;

    public StokController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [Route("{brgId}/{layananId}")]
    public async Task<IActionResult> ListData(string brgId, string layananId)
    {
        var query = new StokGetKartuStokQuery(brgId, layananId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}