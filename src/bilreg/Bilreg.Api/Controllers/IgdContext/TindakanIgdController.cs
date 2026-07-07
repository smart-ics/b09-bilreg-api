using Bilreg.Api.Helpers;
using Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.IgdContext;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
public class TindakanIgdController : Controller
{
    private readonly IMediator _mediator;

    public TindakanIgdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{visitId}")]
    public async Task<IActionResult> AddTindakan(string visitId, [FromBody] AddTindakanBody body)
    {
        var cmd = new IgdTindakanAddCmd(visitId, body.PetugasMedisId,body.ReffId, body.Descriptions, body.Qty, body.Aktifitas,  body.UserId);
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpDelete]
    [Route("delete/{tindakanIgdId}")]
    public async Task<IActionResult> Void(string tindakanIgdId, VoidTIndakanIgdReq req)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);
        var cmd = new IgdTindakanVoidCmd(tindakanIgdId, req.UserId, req.VoidReason, remoteIpAddress, userAgent);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Ok"));
    }

    [HttpGet]
    [Route("list/{visitId}")]
    public async Task<IActionResult> ListData(string visitId)
    {
        var query = new IgdTindakanListQuery(visitId);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
}

public record AddTindakanBody(string ReffId, string Descriptions, int Qty, int Aktifitas, string PetugasMedisId, string UserId);
public record VoidTIndakanIgdReq(string UserId, string VoidReason);