using Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TransportSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class TujuanTransportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TujuanTransportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(TujuanTransportSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetDefaultAmbulance")]
        public async Task<IActionResult> SetDefaultAmbulance(TujuanTransportSetDefaultAmbulanceCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("UnSetDefaultAmbulance/{id}")]
        public async Task<IActionResult> UnSetDefaultAmbulance(string id)
        {
            var cmd = new TujuanTransportUnSetDefaultAmbulanceCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new TujuanTransportDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new TujuanTransportGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new TujuanTransportListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
