using Bilreg.Application.AdmisiContext.RegFeature;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RegSub
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class KarcisController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KarcisController(IMediator mediator)
        {
            _mediator = mediator;
        }

        //[HttpPost]
        //public async Task<IActionResult> Save(KarcisSaveCommand cmd)
        //{
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("SetDefaultTarif")]
        //public async Task<IActionResult> SetDefaultTarif(KarcisSetDefaultTarifCommand cmd)
        //{
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("AddKomponen")]
        //public async Task<IActionResult> AddKomponen(KarcisAddKomponenCommand cmd)
        //{
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("RemoveKomponen")]
        //public async Task<IActionResult> RemoveKomponen(KarcisRemoveKomponenCommand cmd)
        //{
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("AddLayanan")]
        //public async Task<IActionResult> AddLayanan(KarcisAddLayananCommand cmd)
        //{
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("RemoveLayanan")]
        //public async Task<IActionResult> RemoveLayanan(KarcisRemoveLayananCommand cmd)
        //{
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("Activate/{id}")]
        //public async Task<IActionResult> Activate(string id)
        //{
        //    var cmd = new KarcisActivateCommand(id);
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        //[HttpPut]
        //[Route("Deactivate/{id}")]
        //public async Task<IActionResult> Deactivate(string id)
        //{
        //    var cmd = new KarcisDeactivateCommand(id);
        //    await _mediator.Send(cmd);
        //    return Ok(new JSendOk("Done"));
        //}

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new KarcisGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        [Route("list/{instalasiDkId}")]
        public async Task<IActionResult> ListData(string instalasiDkId)
        {
            var query = new KarcisListQuery(instalasiDkId);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        [Route("{layananId}/list")]
        public async Task<IActionResult> ListByLayanan(string layananId)
        {
            var query = new KarcisListByLayananQuery(layananId);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
