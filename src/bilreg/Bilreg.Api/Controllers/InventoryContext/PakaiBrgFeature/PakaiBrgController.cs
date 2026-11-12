using Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.InventoryContext.PakaiBrgFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PakaiBrgController : ControllerBase
{
    private readonly IMediator _mediator;

    public PakaiBrgController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PakaiBrgCreateCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch("void")]
    public async Task<IActionResult> Void([FromBody] PakaiBrgVoidCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var query = new PakaiBrgGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("list/{layananId}/{tglYmd1}/{tglYmd2}")]
    public async Task<IActionResult> ListByLayanan(string layananId, string tglYmd1, string tglYmd2)
    {
        var query = new PakaiBrgListByLayananQuery(layananId, tglYmd1, tglYmd2);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("stok/{layananId}")]
    public async Task<IActionResult> ListStokLokasi(string layananId, [FromQuery] string? keyword = null)
    {
        var query = new PakaiBrgListStokLokasiQuery(layananId, keyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
