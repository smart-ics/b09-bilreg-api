using Farinv.Application.BrgContext.KlasifikasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.BrgContext.KlasifikasiFeature;

[Route("api/[controller]")]
public class GolonganController : Controller
{
    private readonly IMediator _mediator;

    public GolonganController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new GolonganListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
