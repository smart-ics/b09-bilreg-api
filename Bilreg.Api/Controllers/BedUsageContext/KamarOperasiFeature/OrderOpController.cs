using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;

[Route("api/[controller]")]
[ApiController]
public class OrderOpController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderOpController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Route("CreateByReg")]
    public async Task<IActionResult> CreateOrderByReg(OkCreateOrderOpByRegCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpPost]
    [Route("CreateByPasien")]
    public async Task<IActionResult> CreateOrderByPasien(OkCreateOrderOpByPasienCmd cmd)
    {
        var response = await _mediator.Send(cmd);
        return Ok(new JSendOk(response));
    }

    [HttpGet]
    [Route("List")]
    public async Task<IActionResult> ListOrder()
    {
        var baseDate = DateTime.Now.Date; // current state date (today)

        var fakeData = new List<OrderOpResponse>
        {
            new("OP-20251118-001", "PSN-0001", "Andi Setiawan",       "DR-0101", "dr. Budi Santoso, Sp.B",          baseDate.AddHours( 8).AddMinutes(30), "Requested"),
            new("OP-20251118-002", "PSN-0002", "Maria Fransiska",    "DR-0044", "dr. Rini Kartika, Sp.OG",         baseDate.AddHours( 9).AddMinutes(15), "Requested"),
            new("OP-20251118-003", "PSN-0003", "Joko Prabowo",       "DR-0302", "dr. Andhika Putra, Sp.An",        baseDate.AddHours(10).AddMinutes( 0), "Scheduled"),
            new("OP-20251118-004", "PSN-0004", "Siti Aminah",        "DR-0223", "dr. Linda Wijaya, Sp.THT",        baseDate.AddHours(11).AddMinutes(45), "Scheduled"),
            new("OP-20251118-005", "PSN-0005", "Hadi Suganda",       "DR-0188", "dr. Ferry Hartanto, Sp.OT",       baseDate.AddHours(13).AddMinutes(30), "PreOpCleared"),
            new("OP-20251118-006", "PSN-0006", "Kevin Jonathan",     "DR-0155", "dr. Dimas Prasetyo, Sp.U",        baseDate.AddHours(14).AddMinutes(15), "PreOpCleared"),
            new("OP-20251118-007", "PSN-0007", "Lidya Manopo",       "DR-0271", "dr. Meylani Kartono, Sp.M",       baseDate.AddHours(15).AddMinutes(50), "OpStarted"),
            new("OP-20251118-008", "PSN-0008", "Samuel Hutapea",     "DR-0199", "dr. Oskar Simanjuntak, Sp.KJ",    baseDate.AddHours( 8).AddMinutes(50), "OpStarted"),
            new("OP-20251118-009", "PSN-0009", "Nina Kartika",       "DR-0144", "dr. Elvina Pranoto, Sp.PD",       baseDate.AddHours( 9).AddMinutes(40), "RecoveryStarted"),
            new("OP-20251118-010", "PSN-0010", "Agus Wibowo",        "DR-0254", "dr. Indra Wijaya, Sp.A",          baseDate.AddHours(10).AddMinutes(20), "RecoveryStarted"),

            new("OP-20251118-011", "PSN-0011", "Dewi Lestari",       "DR-0099", "dr. Melani Wijayanti, Sp.Rad",    baseDate.AddHours(11).AddMinutes(10), "Requested"),
            new("OP-20251118-012", "PSN-0012", "Rizky Ramadhan",     "DR-0128", "dr. Satriyo Adi, Sp.KFR",         baseDate.AddHours(12).AddMinutes(55), "Scheduled"),
            new("OP-20251118-013", "PSN-0013", "Veronika Samosir",   "DR-0066", "dr. Debora Natalia, Sp.N",        baseDate.AddHours(14).AddMinutes(20), "PreOpCleared"),
            new("OP-20251118-014", "PSN-0014", "Slamet Riyadi",      "DR-0038", "dr. Dedi Hernawan, Sp.JP",        baseDate.AddHours(15).AddMinutes(10), "OpStarted"),
            new("OP-20251118-015", "PSN-0015", "Cornelia Tan",       "DR-0174", "dr. Yessica Tan, Sp.KK",          baseDate.AddHours(16).AddMinutes(25), "RecoveryStarted"),

            new("OP-20251118-016", "PSN-0016", "Bambang Sutikno",    "DR-0211", "dr. Fendy Putra, Sp.BTKV",        baseDate.AddHours( 8).AddMinutes(20), "Requested"),
            new("OP-20251118-017", "PSN-0017", "Yohana Laurenti",    "DR-0119", "dr. Felicia Grace, Sp.OG",        baseDate.AddHours( 9).AddMinutes(10), "Scheduled"),
            new("OP-20251118-018", "PSN-0018", "Mario Santoso",      "DR-0087", "dr. Daniel Wirawan, Sp.OT",       baseDate.AddHours(10).AddMinutes(40), "PreOpCleared"),
            new("OP-20251118-019", "PSN-0019", "Diana Kusuma",       "DR-0055", "dr. Jessica Liem, Sp.THT",        baseDate.AddHours(12).AddMinutes(30), "OpStarted"),
            new("OP-20251118-020", "PSN-0020", "Yonathan Kristanto", "DR-0014", "dr. Kristopher Ang, Sp.An",       baseDate.AddHours(14).AddMinutes(50), "RecoveryStarted")
        };

        return Ok(new { data = fakeData });
    }

}
public record OrderOpResponse(
    string OrderId,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    DateTime TglOperasi,
    string StatusOrderOp
);
