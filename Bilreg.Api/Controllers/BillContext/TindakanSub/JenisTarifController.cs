using Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub;

[Route("api/[controller]")]
[ApiController]
public class JenisTarifController : ControllerBase
{
    private readonly IMediator _mediator;

    public JenisTarifController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Save(JenisTarifSaveCommand cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var cmd = new JenisTarifDeleteCommand(id);
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetData(string id)
    {
        var query = new JenisTarifGetQuery(id);
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    public async Task<IActionResult> ListData()
    {
        var query = new JenisTarifListQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }
}
