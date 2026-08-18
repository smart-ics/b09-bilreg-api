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
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        //var query = new PakaiBedGetQuery(id); 
        //var response = await _mediator.Send(query);
        //return Ok(new JSendOk(response));
        throw new NotImplementedException("GetData method is not implemented yet.");
    }
    [HttpGet]
    [Route("list")]
    public async Task<IActionResult> ListData(string regId)
    {
        //var query = new PakaiBedListQuery(); 
        //var response = await _mediator.Send(query);
        //return Ok(new JSendOk(response));
        throw new NotImplementedException("ListData method is not implemented yet.");
    }

}