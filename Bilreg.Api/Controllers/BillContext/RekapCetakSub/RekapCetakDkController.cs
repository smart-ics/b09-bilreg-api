using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RekapCetakSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class RekapCetakDkController : ControllerBase
    {
        private readonly IMediator _mediator;
        public RekapCetakDkController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRekapCetakDkById(string id)
        {
            var query = new RekapCetakDkGetQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }

        [HttpGet]
        public async Task<IActionResult> ListRekapCetakDk()
        {
            var query = new RekapCetakDkListQuery();
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }
    }
}
