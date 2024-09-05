using Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TransportSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmbulanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AmbulanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(AmbulanceSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPost]
        [Route("AddKomponen")]
        public async Task<IActionResult> AddKomponen(AmbulanceAddKomponenCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPost]
        [Route("RemoveKomponen")]
        public async Task<IActionResult> RemoveKomponen(AmbulanceRemoveKomponenCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new AmbulanceActivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new AmbulanceDeactivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new AmbulanceGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new AmbulanceListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
