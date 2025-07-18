// using Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;
// using MediatR;
// using Microsoft.AspNetCore.Mvc;
// using Nuna.Lib.ActionResultHelper;
//
// namespace Bilreg.Api.Controllers.PasienContext.DemografiSub;
//
// [Route("api/[controller]")]
// [ApiController]
// public class KecamatanController : ControllerBase
// {
//     private readonly IMediator _mediator;
//
//     public KecamatanController(IMediator mediator)
//     {
//         _mediator = mediator;
//     }
//
//     [HttpGet]
//     [Route("{id}")]
//     public async Task<IActionResult> GetData(string id)
//     {
//         var query = new KecamatanGetQuery(id);
//         var response = await _mediator.Send(query);
//         return Ok(new JSendOk(response));
//     }
//
//     [HttpGet]
//     [Route("List/{kabupatenId}")]
//     public async Task<IActionResult> ListData(string kabupatenId)
//     {
//         var query = new KecamatanListQuery(kabupatenId);
//         var response = await _mediator.Send(query);
//         return Ok(new JSendOk(response));
//     }
// }