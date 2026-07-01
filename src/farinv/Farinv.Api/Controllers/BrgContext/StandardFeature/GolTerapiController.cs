using Farinv.Application.BrgContext.StandardFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.BrgContext.StandardFeature;

[Route("api/[controller]")]
[ApiController]
public class GolTerapiController : Controller
{
    private readonly IMediator _mediator;

    public GolTerapiController(IMediator mediator)
    {
        _mediator = mediator; 
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new GolTerapiListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
