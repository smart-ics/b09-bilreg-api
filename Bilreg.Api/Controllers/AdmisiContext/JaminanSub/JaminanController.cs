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

        [HttpPost]
        public async Task<IActionResult> Save(JaminanSaveCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("Activate/{id}")]
        public async Task<IActionResult> Activate(string id)
        {
            var cmd = new JaminanActivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }
        
        [HttpPut]
        [Route("Deactivate/{id}")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var cmd = new JaminanDeactivateCommand(id);
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetAlamat")]
        public async Task<IActionResult> SetAlamat(JaminanSetAlamatCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetCaraBayar")]
        public async Task<IActionResult> SetCaraBayar(JaminanSetCaraBayarCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetGrupJaminan")]
        public async Task<IActionResult> SetGrupJaminan(JaminanSetGrupJaminanCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
        }

        [HttpPut]
        [Route("SetBenefitMou")]
        public async Task<IActionResult> SetBenefitMou(JaminanSetBenefitMouCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok(new JSendOk("Done"));
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
    }
}
