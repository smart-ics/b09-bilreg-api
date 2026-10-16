using Bilreg.Api.Helpers;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.PakaiBedFeature;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PakaiBedController : ControllerBase
{
    private readonly IMediator _mediator;

    public PakaiBedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("")]
    [Route("Create")]
    public async Task<IActionResult> Create(PakaiBedCreateCommand cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("release")]
    public async Task<IActionResult> Release(PakaiBedReleaseRequest req)
    {
        if (req is null)
            return BadRequest(new JSendFailed(new ArgumentNullException(nameof(req), "Payload release tidak boleh kosong.")));

        var cmd = new PakaiBedReleaseCommand(req.PakaiBedId, req.TglJamOut, req.Reason, req.PetugasOut);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("batal")]
    public async Task<IActionResult> Batal(PakaiBedVoidRequest req)
    {
        if (req is null)
            return BadRequest(new JSendFailed(new ArgumentNullException(nameof(req), "Payload void tidak boleh kosong.")));

        try
        {
            var userAgent = HttpContext != null && Request != null ? HttpHelper.GetUserAgent(Request) : string.Empty;
            var remoteIpAddress = HttpContext != null && Request != null ? HttpHelper.GetIpAddress(Request, HttpContext) : "0.0.0.0";
            var cmd = new PakaiBedVoidCommand(req.PakaiBedId, req.UserId, req.VoidReason, userAgent, remoteIpAddress);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new JSendFailed(ex));
        }
    }

    [HttpGet]
    [Route("bed/{bedId}/active")]
    public async Task<IActionResult> GetActiveByBed(string bedId)
    {
        var query = new PakaiBedGetActiveByBedQuery(bedId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("bangsal/{bangsalId}/active")]
    public async Task<IActionResult> ListActiveByWard(string bangsalId)
    {
        var query = new PakaiBedListActiveByWardQuery(bangsalId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{pakaiBedId}")]
    public async Task<IActionResult> GetData(string pakaiBedId)
    {
        var query = new PakaiBedGetQuery(pakaiBedId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("{regId}/list")]
    public async Task<IActionResult> ListDataByReg(string regId)
    {
        var query = new PakaiBedListByRegQuery(regId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    public record PakaiBedVoidRequest(string PakaiBedId, string UserId, string VoidReason);
    public record PakaiBedReleaseRequest(string PakaiBedId, DateTime TglJamOut, string Reason, string PetugasOut);
}