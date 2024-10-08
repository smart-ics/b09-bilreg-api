﻿using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.AdmisiContext.RujukanSub
{
    [Route("api/[controller]")]
    [ApiController]
    public class KelasRujukanController : ControllerBase
    {
        private readonly IMediator _mediator;

        public KelasRujukanController(IMediator mediator)
        {
            this._mediator = mediator;
        }


        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetData(string id)
        {
            var query = new KelasRujukanGetQuery(id);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }

        [HttpGet]
        public async Task<IActionResult> ListData()
        {
            var query = new KelasRujukanListQuery();
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }
    }
}
