using Bilreg.Application.PasienContext.StatusSosialSub.SukuAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.StatusSosialSub;

[Route("api/[controller]")]
[ApiController]
public class SukuController : ControllerBase
{
    private readonly IMediator _mediator;

    public SukuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Save(SukuSaveCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _mediator.Send(new SukuDeleteCommand(id));
        return Ok(new JSendOk("Done"));
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var result = await _mediator.Send(new SukuGetQuery(id));
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var result = await _mediator.Send(new SukuListQuery());
        return Ok(new JSendOk(result));
    }
        
}