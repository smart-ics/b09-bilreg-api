using Bilreg.Application.AdmPasienContext.PekerjaanAgg;
using Bilreg.Application.AdmPasienContext.PekerjaanContext;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.AdmisiSubModul.PasienContext
{
    [Route("api/[controller]")]
    [ApiController]
    public class PekerjaanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PekerjaanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(PekerjaanSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _mediator.Send(new PekerjaanDeleteCommand(id));
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var result = await _mediator.Send(new PekerjaanGetQuery(id));
            return Ok(new JSendOk(result));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var result = await _mediator.Send(new PekerjaanListQuery());
            return Ok(new JSendOk(result));
        }
    }
}
