using Bilreg.Api.Authorization;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.ChargeContext;

//  M03-F01 P2-S05 — HTTP surface for the follow-up order capability (TD-09/TD-10).
//  Reception confirmation is push-based (TD-07): receiving services push to
//  the confirm endpoint; M03 never reads receiving service internals.
//  Cancellation and the cross-visit supervision list require a role beyond
//  authentication via the repo's existing supervisor policy; the role set
//  itself is a deployment concern (AdmissionQueueApiOptions).
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TindakanLanjutController : Controller
{
    private readonly IMediator _mediator;

    public TindakanLanjutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> Create(TdkCreateTindakanLanjutCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("send")]
    public async Task<IActionResult> Send(TdkSendTindakanLanjutCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpPatch]
    [Route("confirm")]
    public async Task<IActionResult> Confirm(TdkConfirmTindakanLanjutCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPatch]
    [Route("batal")]
    [Authorize(Policy = AdmissionQueueSupervisorOperationPolicies.PolicyName)]
    public async Task<IActionResult> Batal(TdkBatalTindakanLanjutCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("outstanding/{regId}")]
    public async Task<IActionResult> ListOutstanding(string regId)
    {
        var query = new TdkListTindakanLanjutOutstandingCmd(regId);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("outstanding")]
    [Authorize(Policy = AdmissionQueueSupervisorOperationPolicies.PolicyName)]
    public async Task<IActionResult> ListOutstandingAll()
    {
        var query = new TdkListTindakanLanjutOutstandingCmd();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
