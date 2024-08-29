using Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.LayananSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstalasiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InstalasiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(InstalasiSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new InstalasiDeleteCommand(id);
            await _mediator.Send(command);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new InstalasiGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new InstalasiListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }


    }
}
