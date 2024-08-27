using Bilreg.Application.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.JaminanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaraBayarDkController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CaraBayarDkController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new CaraBayarDkListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new CaraBayarDkGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
