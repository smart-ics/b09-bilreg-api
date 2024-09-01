using Bilreg.Application.BillContext.RoomChargeSub.KelasAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RoomChargeSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class KelasController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KelasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(KelasSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new KelasListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpPut]
        public async Task<IActionResult> SetKelasDk(string kelasId,string kelasDkId)
        {
            var cmd = new KelasSetKelasDkCommand(kelasId, kelasDkId);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new KelasSetStatusAktifCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new KelasSetStatusTidakAktifCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
    }
}
