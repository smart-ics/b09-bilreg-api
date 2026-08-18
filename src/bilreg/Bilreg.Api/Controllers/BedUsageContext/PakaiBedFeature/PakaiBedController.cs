using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature.UseCases;
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
    [Route("Create")]
    public async Task<IActionResult> Create(PakaiBedCreateCommand cmd)
    {
        var response = await _mediator.Send(cmd);
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

}