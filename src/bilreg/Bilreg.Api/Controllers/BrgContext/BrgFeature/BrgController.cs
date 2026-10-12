using Bilreg.Application.BrgContext.BrgFeature.UseCases;
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

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] BrgRegisterCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BrgUpdateBody body)
    {
        var cmd = new BrgUpdateCommand(
            id,
            body.BrgName,
            body.KetBarang,
            body.GroupRekId,
            body.PabrikId,
            body.SatuanKecilId,
            body.SatuanBesarId,
            body.Konversi);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetAktif(string id, [FromBody] BrgSetAktifBody body)
    {
        var cmd = new BrgSetAktifCommand(id, body.IsAktif);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(string id)
    {
        var query = new BrgGetDetailQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string keyword,
        [FromQuery] string? groupRekDk = null,
        [FromQuery] bool onlyActive = true)
    {
        var query = new BrgSearchQuery(keyword, groupRekDk, onlyActive);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [Obsolete("Use GET /api/brg/search instead")]
    [HttpGet("{searchKeyword}/search")]
    public async Task<IActionResult> ListData(string searchKeyword)
    {
        var query = new BrgListObatQuery(searchKeyword);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}

public record BrgUpdateBody(
    string BrgName,
    string KetBarang,
    string GroupRekId,
    string PabrikId,
    string SatuanKecilId,
    string SatuanBesarId,
    int Konversi,
    string? BrgId = null
);

public record BrgSetAktifBody(
    bool IsAktif,
    string? BrgId = null
);
