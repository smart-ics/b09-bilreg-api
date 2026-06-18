//  TODO: jude-dev-trs-billing
/// using Bilreg.Application.PaymentContext.TrsBillingFeature;
// using MediatR;
// using Microsoft.AspNetCore.Mvc;
// using Nuna.Lib.ActionResultHelper;
//
// namespace Bilreg.Api.Controllers.PaymentContext;
//
// [Route("api/[controller]")]
// [ApiController]
// public class TrsBillingControllerController : Controller
// {
//     private readonly IMediator _mediator;
//
//     public TrsBillingControllerController(IMediator mediator)
//     {
//         _mediator = mediator;
//     }
//
//     [HttpGet]
//     [Route("list/{regId}")]
//     public async Task<IActionResult> ListBill(string regId)
//     {
//         var query = new TrBListBillingQuery(regId);
//         var response = await _mediator.Send(query);
//         return Ok(new JSendOk(response));
//     }
// }
