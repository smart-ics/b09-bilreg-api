using Bilreg.Api.Helpers;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Services;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TindakanController : Controller
{
    private readonly IMediator _mediator;

    public TindakanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create(TdkCreateTindakanCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    [HttpPost]
    [Route("save")]
    public async Task<IActionResult> SaveTindakan(TdkSaveTindakanCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    [Route("tdkJual/list/{regId}")]
    public async Task<IActionResult> ListTdkJual(string regId)
    {
        var query = new TdkListTindakanJualQuery(regId);
        var response = await _mediator.Send(query); 
        return Ok(new JSendOk(response));
    }
    
    [HttpPatch]
    [Route("batal")]
    public async Task<IActionResult> Batal(TindakanVoidRequest req)
    {
        var userAgent = HttpHelper.GetUserAgent(Request);
        var remoteIpAddress = HttpHelper.GetIpAddress(Request, HttpContext);
        var cmd = new TindakanVoidCmd(req.TindakanId, req.UserId, req.VoidReason, remoteIpAddress, userAgent);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }
}

public record TindakanVoidRequest(string TindakanId, string UserId, string VoidReason);