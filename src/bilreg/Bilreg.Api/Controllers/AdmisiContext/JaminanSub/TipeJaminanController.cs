using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.JaminanSub
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class TipeJaminanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TipeJaminanController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var query = new TipeJaminanGetQuery(id);
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }

        //[HttpGet]
        //public async Task<IActionResult> ListData()
        //{
        //    var query = new TipeJaminanListQuery();
        //    var result = await _mediator.Send(query);
        //    return Ok(new JSendOk(result));
        //}

        //[HttpGet]
        //[Route("{jaminanId}/list")]
        //public async Task<IActionResult> ListTipeJaminan(string jaminanId)
        //{
        //    var query = new TipeJaminaByJaminanListQuery(jaminanId);
        //    var result = await _mediator.Send(query);
        //    return Ok(new JSendOk(result));
        //}

        [HttpGet]
        [Route("search/{keyword}")]
        public async Task<IActionResult> Search(string keyword)
        {
            var query = new TipeJaminanSearchQeury(keyword);
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }
    }
}
