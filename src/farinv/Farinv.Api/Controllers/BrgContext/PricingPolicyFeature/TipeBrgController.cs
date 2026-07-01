using Farinv.Application.BrgContext.PricingPolicyFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;


namespace Farinv.Api.Controllers.BrgContext.PricingPolicyFeature;

[Route("api/[controller]")]
public class TipeBrgController : Controller
{
    private readonly IMediator _mediator;

    public TipeBrgController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new TipeBrgListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
