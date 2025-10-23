using Bilreg.Application.AdmisiContext.SearchPasienSub;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegSub;

[Route("api/[controller]")]
[ApiController]
public class SearchPasienController : Controller
{
    private readonly IMediator _mediator;

    public SearchPasienController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    [Route("deep/{keyword}")]
    public async Task<IActionResult> GetData(string keyword)
    {
        var query = new DeepSearchPasienQuery(keyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("quick/{keyword}")]
    public async Task<IActionResult> QuickSearch(string keyword)
    {
        var query = new QuickSearchPasienQuery(keyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
