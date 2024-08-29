using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RujukanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipeRujukanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeRujukanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(TipeRujukanSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new TipeRujukanDeleteCommand(id);
            await _mediator.Send(command);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new TipeRujukanGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new TipeRujukanListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
