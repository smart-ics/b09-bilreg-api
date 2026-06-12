//  TODO: jude-dev-trs-billing
// using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
// using MediatR;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.OpenApi.Services;
// using Nuna.Lib.ActionResultHelper;
//
// namespace Bilreg.Api.Controllers.ChargeContext;
//
// [Route("api/[controller]")]
// [ApiController]
// public class TindakanController : Controller
// {
//     private readonly IMediator _mediator;
//
//     public TindakanController(IMediator mediator)
//     {
//         _mediator = mediator;
//     }
//
//     [HttpPost]
//     [Route("create")]
//     public async Task<IActionResult> Create(TdkCreateTindakanCmd cmd)
//     {
//         var result = await _mediator.Send(cmd);
//         return Ok(new JSendOk(result));
//     }
//     [HttpPost]
//     [Route("save")]
//     public async Task<IActionResult> SaveTindakan(TdkSaveTindakanCmd cmd)
//     {
//         var result = await _mediator.Send(cmd);
//         return Ok(new JSendOk(result));
//     }
//
//     [HttpGet]
//     [Route("tdkJual/list/{regId}")]
//     public async Task<IActionResult> ListTdkJual(string regId)
//     {
//         var query = new TdkListTindakanJualQuery(regId);
//         var response = await _mediator.Send(query); 
//         return Ok(new JSendOk(response));
//     }
//     [HttpPatch]
//     [Route("batal")]
//     public async Task<IActionResult> Batal(TindakanVoidCmd cmd)
//     {
//         await _mediator.Send(cmd);
//         return Ok(new JSendOk("Done"));
//     }
// }