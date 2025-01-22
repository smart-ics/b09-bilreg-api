using Bilreg.Application.PasienContext.DemografiSub.KotaAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.PasienContext.DemografiSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class KotaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KotaController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new KotaGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new KotaListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
