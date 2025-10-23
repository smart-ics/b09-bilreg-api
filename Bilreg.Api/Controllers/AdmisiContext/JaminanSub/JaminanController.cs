using Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.JaminanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class JaminanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public JaminanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new JaminanGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new JaminanListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        [Route("search/{keyword}")]
        public async Task<IActionResult> Search(string keyword)
        {
            var query = new JaminanSearchQuery(keyword);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
