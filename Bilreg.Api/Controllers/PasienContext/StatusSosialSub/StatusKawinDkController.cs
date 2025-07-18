// using Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg;
// using MediatR;
// using Microsoft.AspNetCore.Mvc;
// using Nuna.Lib.ActionResultHelper;
//
// namespace Bilreg.Api.Controllers.PasienContext.StatusSosialSub;
//
// [Route("api/[controller]")]
// [ApiController]
// public class StatusKawinDkController : ControllerBase
// {
//
//     private readonly IMediator _mediator;
//
//     public StatusKawinDkController(IMediator mediator)
//     {
//         _mediator = mediator;
//     }
//
//     [HttpGet]
//     public async Task<IActionResult> ListData()
//     {
//         var cmd = new StatusKawinDkListQuery();
//         var result = await _mediator.Send(cmd);
//         return Ok(new JSendOk(result));
//     }
//
//     [HttpGet]
//     [Route("{id}")]
//     public async Task<IActionResult> GetData(string id)
//     {
//         var cmd = new StatusKawinDkGetQuery(id);
//         var result = await _mediator.Send(cmd);
//         return Ok(new JSendOk(result));
//     }
// }