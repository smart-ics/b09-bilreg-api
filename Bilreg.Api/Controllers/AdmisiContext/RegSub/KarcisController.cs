using Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class KarcisController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KarcisController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(KarcisSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetDefaultTarif")]
        public async Task<IActionResult> SetDefaultTarif(KarcisSetDefaultTarifCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("AddKomponen")]
        public async Task<IActionResult> AddKomponen(KarcisAddKomponenCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("RemoveKomponen")]
        public async Task<IActionResult> RemoveKomponen(KarcisRemoveKomponenCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("AddLayanan")]
        public async Task<IActionResult> AddLayanan(KarcisAddLayananCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("RemoveLayanan")]
        public async Task<IActionResult> RemoveLayanan(KarcisRemoveLayananCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new KarcisActivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new KarcisDeactivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new KarcisGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new KarcisListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
