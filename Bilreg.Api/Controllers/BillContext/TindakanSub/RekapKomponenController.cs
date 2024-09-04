using Bilreg.Application.BillContext.TindakanSub.RekapKomponenAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class RekapKomponenController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RekapKomponenController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(RekapKomponenSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRekapKomponenById(string id)
        {
            var query = new RekapKomponenGetQuery(id);
            var response = _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new RekapKomponenListQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new RekapKomponenDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
    }
}
