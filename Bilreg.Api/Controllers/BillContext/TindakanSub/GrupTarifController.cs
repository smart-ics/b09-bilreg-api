using Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupTarifController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GrupTarifController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(GrupTarifSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new GrupTarifDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new GrupTarifGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new GrupTarifListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
