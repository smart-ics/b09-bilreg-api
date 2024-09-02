using Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RujukanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class RujukanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RujukanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(RujukanSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new RujukanActivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new RujukanDeactivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetAlamat")]
        public async Task<IActionResult> SetAlamat(RujukanSetAlamatCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetNoTelp")]
        public async Task<IActionResult> SetNoTelp(RujukanSetNoTelpCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetTipeRujukan")]
        public async Task<IActionResult> SetTipeRujukan(RujukanSetTipeRujukanCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetKelasRujukan")]
        public async Task<IActionResult> SetKelasRujukan(RujukanSetKelasRujukanCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetCaraMasukDk")]
        public async Task<IActionResult> SetCaraMasukDk(RujukanSetCaraMasukDkCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new RujukanGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new RujukanListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }

}
