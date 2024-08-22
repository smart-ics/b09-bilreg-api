using Bilreg.Application.AdmPasienContext.AgamaContext;
using Bilreg.Application.AdmPasienContext.StatusKawinAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.AdmisiSubModul.PasienContext
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusKawinController : ControllerBase
    {

        private readonly IMediator _mediator;

        public StatusKawinController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(StatusKawinSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new StatusKawinDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var cmd = new StatusKawinListQuery();
            var result = await _mediator.Send(cmd);
            return Ok(new JSendOk(result));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var cmd = new StatusKawinGetQuery(id);
            var result = await _mediator.Send(cmd);
            return Ok(new JSendOk(result));
        }

    }
}
