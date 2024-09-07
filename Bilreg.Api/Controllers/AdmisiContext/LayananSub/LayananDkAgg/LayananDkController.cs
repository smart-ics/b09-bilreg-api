using Bilreg.Application.AdmisiContext.LayananSub.LayananDkAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.LayananSub.LayananDkAgg
{
    [Route("api/[controller]")]
    [ApiController]
    public class LayananDkController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LayananDkController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLayananDk(string id)
        {
            var query = new LayananDkGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new LayananDkListQuery();
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }
    }
}
