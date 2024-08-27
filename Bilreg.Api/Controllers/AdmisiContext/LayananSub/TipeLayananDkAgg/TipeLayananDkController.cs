using Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.LayananSub.TipeLayananDkAgg
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipeLayananDkController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeLayananDkController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new TipeLayananDkGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new TipeLayananDkListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
