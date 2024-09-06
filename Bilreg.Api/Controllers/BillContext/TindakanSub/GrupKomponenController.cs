using Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg;
using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupKomponenController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GrupKomponenController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(GrupKomponenSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new GrupKomponenDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGrupKomponenById(string id)
        {
            var query = new GrupKomponenGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new GrupKomponenListQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }

    }
}
