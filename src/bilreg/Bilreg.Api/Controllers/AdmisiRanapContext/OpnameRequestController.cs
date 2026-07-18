using Bilreg.Api.Filters;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiRanapContext;

[Route("api/admisi-ranap/opname-request")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]
public class OpnameRequestController : ControllerBase
{
    private readonly IMediator _mediator;

    public OpnameRequestController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdmCreateOpnameRequestCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, [FromBody] AdmCancelOpnameRequestBody body)
    {
        await _mediator.Send(new AdmCancelOpnameRequestCmd(id, body.UserId));
        return Ok(new JSendOk("Done"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var result = await _mediator.Send(new AdmGetOpnameRequestQry(id));
        return Ok(new JSendOk(result));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] OpnameRequestStatusEnum? status)
    {
        var result = await _mediator.Send(new AdmListOpnameRequestQry(status));
        return Ok(new JSendOk(result));
    }
}

public record AdmCancelOpnameRequestBody(string UserId);
