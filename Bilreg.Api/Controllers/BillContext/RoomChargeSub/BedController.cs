using Bilreg.Application.BillContext.RoomChargeSub.BedAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.RoomChargeSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class BedController : Controller
    {
        private readonly IMediator _mediator;
        public BedController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Save(BedSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var cmd = new BedDeleteCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new BedActivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new BedDeactivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new BedGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response)); 
        }

        [HttpGet]
        [Route("ByIdBangsal{BangsalId}")]
        public async Task<IActionResult> ListDataByIdBangsal(string BangsalId) 
        {
            var query = new BedListDataByBangsalId(BangsalId);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response)); 
        }
        
    }
}
