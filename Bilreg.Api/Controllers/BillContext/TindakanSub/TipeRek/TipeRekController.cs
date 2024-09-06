using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRek;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub.TipeRek
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipeRekController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeRekController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetTipeRekId(string id)
        {
            var query = new TipeRekGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new TipeRekListQuery();
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }
    }
}
