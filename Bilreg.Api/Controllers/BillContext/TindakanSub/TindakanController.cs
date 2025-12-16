using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BillContext.TindakanSub;

[Route("api/[controller]")]
[ApiController]
public class TindakanController : Controller
{
    private readonly IMediator _mediator;

    public TindakanController(IMediator mediator)
    {
        _mediator = mediator;
    }
    #region Order
    [HttpPost]
    [Route("order")]
    public async Task<IActionResult> CreateOrder(OrderTdkCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    [HttpPost]
    [Route("orderWithoutReg")]
    public async Task<IActionResult> CreateOrderWithoutReg(OrderTdkCreateWithoutRegCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }

    [HttpPut]
    [Route("cancelOrderTdk")]
    public async Task<IActionResult> CancelOrder(OrderTdkCancelCmd cmd)
    {
        await _mediator.Send(cmd);
        return Ok(new JSendOk("Done"));
    }


    [HttpGet]
    [Route("order/{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        var query = new OrderTindakanGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
    #endregion

    #region Tindakan

    [HttpPost]
    public async Task<IActionResult> CreateTindaka(TindakanCreateCmd cmd)
    {
        var result = await _mediator.Send(cmd);
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetTindakan(string id)
    {
        var query = new TindakanGetQuery(id);
        var result = await _mediator.Send(query);
        return Ok(new JSendOk(result));
    }
    [HttpGet]
    [Route("list/{regId}/{layananId}")]
    public async Task<IActionResult> ListTindakan(string regId, string layananId)
    {
        var fakerData = new List<ResponseOrderTdk>
        {
            new ResponseOrderTdk(
                OrderId: "ORD-202501-001",
                OrderDate: "2025-01-12 08:15:00",
                DokterOrderId: "D00123",
                DokterOrderName: "Dr. Budi Santoso, Sp.THT",
                TindakanId: "TDK-1001",
                TindakanDate: "2025-01-12 09:00:00",
                ReffDate: "2025-01-12 09:00:00",
                TarifId: "TRF-5501",
                TarifName: "Pembersihan Telinga",
                Ppa: "Dokter1, Dokter2"
            ),
            new ResponseOrderTdk(
                OrderId: "ORD-202501-002",
                OrderDate: "2025-01-12 10:20:00",
                DokterOrderId: "D00456",
                DokterOrderName: "Dr. Sinta Maharani, Sp.KJ",
                TindakanId: "TDK-2002",
                TindakanDate: "2025-01-12 10:45:00",
                ReffDate: "2025-01-12 10:45:00",
                TarifId: "TRF-6602",
                TarifName: "Konseling Psikiatri",
                Ppa: "Dokter1"
            ),
            new ResponseOrderTdk(
                OrderId: "ORD-202501-003",
                OrderDate: "2025-01-12 13:30:00",
                DokterOrderId: "P00999",
                DokterOrderName: "Ners Rani Putri, S.Kep",
                TindakanId: "",
                TindakanDate: "",
                ReffDate: "2025-01-12 13:30:00",
                TarifId: "TRF-7708",
                TarifName: "Perawatan Luka Ringan",
                Ppa: "Perawat1"
            )
        };
        var result = fakerData
            .OrderBy(x => x.ReffDate).ToList() ?? [];
        return Ok(new JSendOk(fakerData));

    }
    #endregion
    public record ResponseOrderTdk(
        string OrderId ,
        string OrderDate,
        string DokterOrderId,
        string DokterOrderName,
        string TindakanId,
        string TindakanDate,
        string ReffDate, 
        string TarifId,
        string TarifName,
        string Ppa
    );

    
}
