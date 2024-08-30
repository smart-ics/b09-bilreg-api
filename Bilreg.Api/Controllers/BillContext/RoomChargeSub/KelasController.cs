using Bilreg.Application.BillContext.RoomChargeSub.KelasAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RoomChargeSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class KelasController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KelasController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(KelasSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
    }
}
