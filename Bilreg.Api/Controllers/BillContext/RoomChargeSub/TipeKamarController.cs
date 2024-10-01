using Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RoomChargeSub;

    [Route("api/[controller]")]
    [ApiController]
    public class TipeKamarController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeKamarController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]
        public async Task<IActionResult> Save(TipeKamarSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new TipeKamarGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new TipeKamarListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        
        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new TipeKamarSetActive(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new TipeKamarSetDeactive(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("SetDefault/{id}")]
        public async Task<IActionResult> Default(string id)
        {
            var cmd = new TipeKamarSetDefault(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("ResetDefault")]
        public async Task<IActionResult> ResetDefault()
        {
            var cmd = new TipeKamarResetDefault();
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetNoUrut")]
        public async Task<IActionResult> SetNoUrut(TipeKamarSetNoUrut cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
    }