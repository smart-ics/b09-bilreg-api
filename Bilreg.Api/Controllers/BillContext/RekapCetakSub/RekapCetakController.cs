using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RekapCetakSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class RekapCetakController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RekapCetakController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpPost]
        public async Task<IActionResult> Save(RekapCetakSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetNoUrut")]
        public async Task<IActionResult> SetNoUrut(RekapCetakSetNoUrutCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
    
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new RekapCetakGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new RekapCetakListQuery("");
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
