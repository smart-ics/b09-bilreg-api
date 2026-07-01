using Farinv.Application.BrgContext.BrgFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Farinv.Api.Controllers.BrgContext.BrgFeature;

[Route("api/[controller]")]
[ApiController]
public class SatuanController : ControllerBase
{
    private readonly IMediator _mediator;

    public SatuanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new SatuanListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
