 //TODO: Refactor AntrianMap Model

using Bilreg.Api.Helpers;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegFeature;

[Route("api/[controller]")]
[ApiController]
public class RegController : Controller
{
    private readonly IMediator _mediator;
    public RegController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("rajalWalkIn")]
    public async Task<IActionResult> Save(RegJalanWalkInCommand cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost]
    [Route("rajalByBooking")]
    public async Task<IActionResult> Save(RegJalanByBookingCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("setSjpNumber")]
    public async Task<IActionResult> SetNoSep(RegSetNoSjpCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("ubahKunjungan")]
    public async Task<IActionResult> UbahKunjungan(RegJalanUbahKunjunganCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPatch]
    [Route("ubahJaminan")]
    public async Task<IActionResult> UbahJaminan(RegJalanUbahJaminanCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("rajalBatal")]
    public async Task<IActionResult> Void(RegBatalRequestDto req)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);

        var cmd = new RegJalanBatalCmd(req.RegId, req.UserId, req.VoidReason, remoteIpAddress, userAgent);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPost]
    [Route("darurat")]
    public async Task<IActionResult> RegRadar(RegDaruratCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);

        var cmd = new RegRegAktifRemoveCmd(id, remoteIpAddress, userAgent);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new RegGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{layananId}/layanan")]
    public async Task<IActionResult> ListAktif(string layananId)
    {
        var query = new RegAktifLayananListQuery(layananId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("aktif/{pasienId}")]
    public async Task<IActionResult> ListAktifByMr(string pasienId)
    {
        var query = new RegAktifByMrListQuery(pasienId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("aktif/{jenisReg}/jenisReg")]
    public async Task<IActionResult> ListAktifByJenisReg(int jenisReg)
    {
        var query = new RegAktifByJenisRegListQuery(jenisReg);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}

public record RegBatalRequestDto(string RegId, string UserId, string VoidReason);