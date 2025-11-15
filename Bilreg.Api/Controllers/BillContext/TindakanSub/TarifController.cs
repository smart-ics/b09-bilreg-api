using Bilreg.Application.BillContext.TindakanSub.TarifAgg;
using Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class TarifController : Controller
    {
        private readonly IMediator _mediator;

        public TarifController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        [Route("search/{keyword}")]
        public async Task<IActionResult> GetData(string keyword)
        {
            var query = new TarifSearchQuery(keyword);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
