using Farinv.Application.BrgContext.StandardFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.BrgContext.StandardFeature;

[Route("api/[controller]")]
[ApiController]
public class KfaController : Controller
{
    private readonly IMediator _mediator;

    public KfaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("Search")]
    public async Task<IActionResult> SearchData(string keyword)
    {
        var query = new KfaSearchQuery(keyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
