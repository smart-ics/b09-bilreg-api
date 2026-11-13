using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TarifController : Controller
    {
        private readonly IMediator _mediator;

        public TarifController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet]
        [Route("search/{layananId}/{keyword}")]
        public async Task<IActionResult> GetData(string layananId, string keyword)
        {
            var query = new TrfSearchTarifBrgQuery(layananId, keyword);
            var response = await _mediator.Send(query);
            return Ok(new JSendOk(response));
        }


        [HttpGet]
        [Route("nilai/{id}/{tipeTarifId}/{kelasId}")]
        public async Task<IActionResult> GetNilai(
            string id,
            string tipeTarifId,
            string kelasId,
            [FromQuery] string? tipeJaminanId = null,
            [FromQuery] string? layananId = null)
        {
            var query = new TrfGetNilaiTarifQuery(id, tipeTarifId, kelasId, tipeJaminanId ?? string.Empty, layananId ?? string.Empty);
            var result = await _mediator.Send(query);
            return Ok(new JSendOk(result));
        }
    }
}
