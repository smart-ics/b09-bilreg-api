// using Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;
// using MediatR;
// using Microsoft.AspNetCore.Mvc;
// using Nuna.Lib.ActionResultHelper;
//
// namespace Bilreg.Api.Controllers.PasienContext.StatusSosialSub
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class PendidikanDkController : ControllerBase
//     {
//         private readonly IMediator _mediator;
//
//         public PendidikanDkController(IMediator mediator)
//         {
//             _mediator = mediator;
//         }
//         
//         [HttpGet]
//         [Route("{id}")]
//         public async Task<IActionResult> GetData(string id)
//         {
//             var query = new PendidikanDkGetQuery(id);
//             var response = await _mediator.Send(query);
//             return Ok(new JSendOk(response));
//         }
//
//         [HttpGet]
//         public async Task<IActionResult> ListData()
//         {
//             var query = new PendidikanDkListQuery();
//             var response = await _mediator.Send(query);
//             return Ok(new JSendOk(response));
//         }
//     }
// }
