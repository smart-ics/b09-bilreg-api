using Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipeTarifController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeTarifController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(TipeTarifSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new TipeTarifDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new TipeTarifActivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new TipeTarifDeactivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var cmd = new TipeTarifGetQuery(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var cmd = new TipeTarifListQuery();
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
    }
}
