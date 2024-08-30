using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.JaminanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupJaminanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GrupJaminanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(GrupJaminanSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var query = new GrupJaminanDeleteCommand(id);
            await _mediator.Send(query);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new GrupJaminanGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new GrupJaminanListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpPut]
        [Route("SetAsKaryawan/{id}")]
        public async Task<IActionResult> SetAsKaryawan(string id)
        {
            var cmd = new GrupJaminanSetAsKaryawanCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
    }
}
