using Bilreg.Application.SalesContext.ReturJualFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.SalesContext.ReturJualFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReturJualController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReturJualController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateReturJual(ReturJualCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new ReturJualGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("list/{regId}")]
    public async Task<IActionResult> ListDataByRegId(string regId)
    {
        var query = new ReturJualListByRegQuery(regId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
