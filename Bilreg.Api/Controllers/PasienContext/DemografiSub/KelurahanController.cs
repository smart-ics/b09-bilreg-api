using Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.DemografiSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class KelurahanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KelurahanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(KelurahanSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new KelurahanDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new KelurahanGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        [Route("List/{kecamatanId}")]
        public async Task<IActionResult> ListData(string kecamatanId)
        {
            var query = new KelurahanListQuery(kecamatanId);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        [Route("Search/{keyword}")]
        public async Task<IActionResult> SearchData(string keyword)
        {
            var query = new KelurahanSearchQuery(keyword);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
