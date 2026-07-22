using Farinv.Application.BrgContext.BrgFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BrgContext.BrgFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BrgController : ControllerBase
{
    private readonly IMediator _mediator;

    public BrgController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{searchKeyword}/search")]
    public async Task<IActionResult> ListData(string searchKeyword)
    {
        var query = new BrgListObatQuery(searchKeyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
