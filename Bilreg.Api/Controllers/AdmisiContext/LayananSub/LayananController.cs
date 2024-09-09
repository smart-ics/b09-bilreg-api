using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.LayananSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class LayananController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LayananController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]
        public async Task<IActionResult> Save(LayananSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new LayananGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new LayananListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new LayananSetActiveCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new LayananSetDeactiveCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetInstalasiDk/")]
        public async Task<IActionResult> SetInstalasiDk(LayananSetInstalasiDkCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetLayananDk/")]
        public async Task<IActionResult> SetLayananDk(LayananSetLayananDkCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        [HttpPut]
        [Route("SetPetugasMedis/")]
        public async Task<IActionResult> SetPetugasMedis(LayananSetPetugasMedisCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("LayananSetSmf/")]
        public async Task<IActionResult> LayananSetSmf(LayananSetSmfCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        [HttpPut]
        [Route("SetTipeLayananDk/")]
        public async Task<IActionResult> LayananSetTipeLayananDk(LayananSetTipeLayananDkCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));


        }
    }
}
