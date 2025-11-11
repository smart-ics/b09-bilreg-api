using Bilreg.Application.AdmisiContext.BookingFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.BookingFeature;

[Route("api/[controller]")]
[ApiController]
public class JadwalPraktekController : ControllerBase
{
    private readonly IMediator _mediator;
    public JadwalPraktekController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    [Route("{dokterId}")]
    public async Task<IActionResult> ListByDokter(string dokterId)
    {
        var query = new JadwalPraktekListQuery(dokterId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("layanan/{layananId}")]
    public async Task<IActionResult> ListbyLayanan(string layananId)
    {
        var query = new JadwalPraktekListByLayananQuery(layananId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
    
    [HttpGet]
    [Route("layananDk/{layananDkId}")]
    public async Task<IActionResult> ListbyLayananDk(string layananDkId)
    {
        var query = new JadwalPraktekListByLayananDkQuery(layananDkId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("search/{keyword}")]
    public async Task<IActionResult> SearchJadwal(string keyword)
    {
        var query = new JadwalPraktekSearchQuery(keyword);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    public async Task<IActionResult> Save(JadwalPraktekCreateCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }
    
}