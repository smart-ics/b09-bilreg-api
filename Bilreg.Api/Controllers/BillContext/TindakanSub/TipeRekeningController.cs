using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg.TipeRekening;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipeRekeningController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeRekeningController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetTipeRekeningId(string id)
        {
            var query = new TipeRekeningGetQuery(id);
            var response = _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new TipeRekeningListQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }
    }
}
