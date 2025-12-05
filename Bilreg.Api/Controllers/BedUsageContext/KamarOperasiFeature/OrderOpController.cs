using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Nuna.Lib.ActionResultHelper;
using System.Text.Json.Serialization;

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
    public async Task<IActionResult> ListOrder()
    {
        var query = new OkListOperasiAktifQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("ListSchedule/{Tgl1}/{Tgl2}")]
    public async Task<IActionResult> ListSchedule(DateTime Tgl1, DateTime Tgl2)
    {
        var fakedata = GenerateConvertedScheduleData();

        // --- Logika Filtering (Tetap Sama) ---
        var startPeriod = Tgl1.Date.ToUniversalTime();
        var endPeriod = Tgl2.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();

        // Kita filter menggunakan properti Tgl, dan mengonversinya kembali ke DateTime untuk filtering
        var filteredSchedules = fakedata
            .Where(s =>
            {
                // Membuat objek DateTime UTC untuk perbandingan filter
                if (DateTime.TryParseExact($"{s.Tgl} {s.Jam}", "yyyy-MM-dd HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime tglJam))
                {
                    return tglJam >= startPeriod && tglJam <= endPeriod;
                }
                return false;
            })
            .OrderBy(s => s.Tgl)
            .ThenBy(s => s.Jam);
        // --- Akhir Logika Filtering ---
        if (!filteredSchedules.Any())
        {
            return NotFound($"Tidak ada jadwal operasi antara {Tgl1.ToShortDateString()} dan {Tgl2.ToShortDateString()}.");
        }
        return Ok(new JSendOk(filteredSchedules.ToList()));
    }

    private List<Schedule> GenerateConvertedScheduleData()
    {
        var schedules = new List<Schedule>();

        string[] dokterNames = { "Dr. Siti Rahayu, Sp.B", "Dr. Rahmat Hidayat, Sp.M", "Dr. Adelia Putri, Sp.OT" };
        string[] kamarNames = { "OK-1A", "OK-2B", "OK-3C" };

        // PERBAIKAN: Pastikan TIPE data pada deklarasi tuple ini benar-benar cocok dengan 9 nilai di bawah.
        var dataPairs = new (string PasienId, string PasienName, string NamaOperasi,
                             string TglLahir, int DayOffset, int Hour, int Minute,
                             UrgencyLevelEnum Urgency, int DurationMinutes)[]
        {
                // Pasangan data:
                // ID, Nama, Operasi, TglLahir, DayOffset, Hour, Minute, Urgency (Enum), DURASI (int Menit)
                ("P001245", "Anugrah Putra", "Apendektomi", "1995-04-10", 0, 9, 0, UrgencyLevelEnum.Emergency, 90),
                ("P005872", "Bunga Lestari", "Katarak (OD)", "2001-11-20", 0, 11, 0, UrgencyLevelEnum.Elective, 45),
                ("P001901", "Cahyo Adi", "Herniorafi Inguinal", "1978-07-25", 1, 8, 30, UrgencyLevelEnum.Urgent, 120),
                ("P003344", "Dewi Sartika", "Kolekistektomi Laparoskopi", "1965-01-15", 1, 13, 0, UrgencyLevelEnum.Elective, 60),
                ("P007651", "Eko Susilo", "Repair Fraktur Tibia", "1999-03-05", 2, 9, 30, UrgencyLevelEnum.Urgent, 180),
                ("P004123", "Fajar Mubarok", "Tonsilektomi", "2010-09-18", 3, 10, 0, UrgencyLevelEnum.Elective, 60),
                ("P009988", "Gita Paramita", "Histerektomi", "1984-12-30", 3, 13, 30, UrgencyLevelEnum.Elective, 150),
                ("P002211", "Hadi Jaya", "Angiografi Koroner", "1950-02-01", 4, 8, 0, UrgencyLevelEnum.Urgent, 90),
                ("P006050", "Intan Permata", "Sesar (Sectio Caesarea)", "1990-06-06", 4, 14, 0, UrgencyLevelEnum.Emergency, 60),
                ("P008432", "Joko Santoso", "Transurethral Resection of the Prostate (TURP)", "1973-10-10", 7, 9, 0, UrgencyLevelEnum.Elective, 120),
                ("P001199", "Kartika Sari", "Pemasangan Ventilator", "2015-05-22", 7, 11, 30, UrgencyLevelEnum.Crash, 30),
                ("P005577", "Lukman Hakim", "Debridemen Luka Bakar", "1988-08-08", 8, 8, 0, UrgencyLevelEnum.Urgent, 105),
                ("P003020", "Maya Dewi", "Eksplorasi Laparotomi", "1997-02-14", 9, 7, 30, UrgencyLevelEnum.Emergency, 150),
                ("P004848", "Nugroho Jati", "Total Knee Replacement (TKR)", "1960-04-01", 9, 11, 0, UrgencyLevelEnum.Elective, 240),
                ("P007010", "Olivia Puspita", "Mastektomi Radikal", "1993-06-28", 11, 10, 0, UrgencyLevelEnum.Urgent, 210)
        };

        var startDate = new DateTime(2025, 11, 3, 0, 0, 0, DateTimeKind.Utc);
        var random = new Random();

        for (int i = 0; i < dataPairs.Length; i++)
        {
            var pair = dataPairs[i];

            var opDate = startDate.AddDays(pair.DayOffset);
            var tglJam = new DateTime(opDate.Year, opDate.Month, opDate.Day, pair.Hour, pair.Minute, 0, DateTimeKind.Utc);

            schedules.Add(new Schedule
            {
                PasienId = pair.PasienId,
                PasienName = pair.PasienName,
                PasienTglLahir = DateTime.Parse(pair.TglLahir).ToString("yyyy-MM-dd"),
                NamaOperasi = pair.NamaOperasi,
                Urgenitas = pair.Urgency,

                // PENGGUNAAN: Mengambil nilai integer Durasi dari tuple
                Durasi = pair.DurationMinutes,

                KamarName = kamarNames[random.Next(kamarNames.Length)],
                Tgl = tglJam.ToString("yyyy-MM-dd"),
                Jam = tglJam.ToString("HH:mm"),
                DokterName = dokterNames[random.Next(dokterNames.Length)]
            });
        }

        return schedules;
    }
}

public class Schedule
{
    public string PasienId { get; set; }
    public string PasienName { get; set; }
    public string PasienTglLahir { get; set; }
    public string NamaOperasi { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UrgencyLevelEnum Urgenitas { get; set; }

    public int Durasi { get; set; }
    public string KamarName { get; set; }
    public string Tgl { get; set; }
    public string Jam { get; set; }
    public string DokterName { get; set; }
}