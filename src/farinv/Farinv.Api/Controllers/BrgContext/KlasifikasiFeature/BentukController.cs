using Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.BrgContext.KlasifikasiFeature;

[Route("api/[controller]")]
public class BentukController : Controller
{
    private readonly IMediator _mediator;

    public BentukController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new BentukListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
