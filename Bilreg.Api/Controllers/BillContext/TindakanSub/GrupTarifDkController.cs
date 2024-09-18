using Bilreg.Application.BillContext.TindakanSub.GrupTarifDkAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupTarifDkController : ControllerBase
    {
        private readonly IMediator _mediator;

        public GrupTarifDkController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new GrupTarifDkGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new GrupTarifDkListQuery();
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }
    }
}