using Bilreg.Application.AdmPasienContext.AgamaContext;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.AdmisiSubModul.PasienContext
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgamaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AgamaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(AgamaSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new AgamaDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var cmd = new AgamaGetQuery(id);
            var result = await _mediator.Send(cmd);
            return Ok(new JSendOk(result));
        }
        
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var cmd = new AgamaListQuery();
            var result = await _mediator.Send(cmd);
            return Ok(new JSendOk(result));
        }
    }

}

