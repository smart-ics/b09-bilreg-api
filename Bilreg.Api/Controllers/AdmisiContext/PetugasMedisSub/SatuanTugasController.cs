using Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.PetugasMedisSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class SatuanTugasController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SatuanTugasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(SatuanTugasSaveCommand cmd)
        {
            // The command now includes a boolean field "IsMedis" that needs to be handled
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new SatuanTugasDeleteCommand(id);
            await _mediator.Send(command);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new SatuanTugasGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new SatuanTugasListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
