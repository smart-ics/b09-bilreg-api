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
    public async Task<IActionResult> ListOrder()
    {
        var query = new OkListOperasiAktifQuery();
        var response = await _mediator.Send(query);
        return Ok(new JSendOk(response));
    }

    [HttpGet("ListSchedule/{Tgl1}/{Tgl2}")]
    public async Task<IActionResult> ListSchedule(DateTime Tgl1, DateTime Tgl2)
    {
        var fakedata = GenerateFakeScheduleData();

        // --- Logika Filtering ---
        var filteredSchedules = fakedata.AsEnumerable();

        // 1. Mengambil waktu pada tanggal1 (00:00:00) dan memastikan zona waktu-nya UTC
        var startPeriod = Tgl1.Date;

        // 2. Mengambil waktu PUKUL 23:59:59 pada tanggal2 dan memastikan zona waktu-nya UTC
        var endPeriod = Tgl2.Date.AddDays(1).AddSeconds(-1);

        // Filter data
        filteredSchedules = filteredSchedules
            .Where(s => s.TglJam >= startPeriod && s.TglJam <= endPeriod)
            .OrderBy(s => s.TglJam);
        // --- Akhir Logika Filtering ---

        if (!filteredSchedules.Any())
        {
            return NotFound($"Tidak ada jadwal operasi antara {Tgl1.ToShortDateString()} dan {Tgl2.ToShortDateString()}.");
        }
        return Ok(new JSendOk(filteredSchedules.ToList()));
    }

    private List<Schedule> GenerateFakeScheduleData()
    {
        var schedules = new List<Schedule>();

        // Definisi data yang akan di-random
        string[] dokterNames = { "Dr. Siti Rahayu, Sp.B", "Dr. Rahmat Hidayat, Sp.M", "Dr. Adelia Putri, Sp.OT" };
        string[] urgenitas = { "Emergensi", "Urgent", "Elektif" };
        string[] kamarNames = { "OK-1A", "OK-2B", "OK-3C" };
        string[] durasi = { "1 jam", "1 jam 30 menit", "2 jam", "2 jam 30 menit", "3 jam", "45 menit", "4 jam" };

        // Data Pasien dan Operasi
        var dataPairs = new (string PasienId, string PasienName, string NamaOperasi, string TglLahir, int DayOffset)[]
        {
                ("P001245", "Anugrah Putra", "Apendektomi", "1995-04-10", 0),  // 3 Nov
                ("P005872", "Bunga Lestari", "Katarak (OD)", "2001-11-20", 0),
                ("P001901", "Cahyo Adi", "Herniorafi Inguinal", "1978-07-25", 1),  // 4 Nov
                ("P003344", "Dewi Sartika", "Kolekistektomi Laparoskopi", "1965-01-15", 1),
                ("P007651", "Eko Susilo", "Repair Fraktur Tibia", "1999-03-05", 2),  // 5 Nov
                ("P004123", "Fajar Mubarok", "Tonsilektomi", "2010-09-18", 3),  // 6 Nov
                ("P009988", "Gita Paramita", "Histerektomi", "1984-12-30", 3),
                ("P002211", "Hadi Jaya", "Angiografi Koroner", "1950-02-01", 4),  // 7 Nov
                ("P006050", "Intan Permata", "Sesar (Sectio Caesarea)", "1990-06-06", 4),
                ("P008432", "Joko Santoso", "Transurethral Resection of the Prostate (TURP)", "1973-10-10", 7),  // 10 Nov
                ("P001199", "Kartika Sari", "Pemasangan Ventilator", "2015-05-22", 7),
                ("P005577", "Lukman Hakim", "Debridemen Luka Bakar", "1988-08-08", 8),  // 11 Nov
                ("P003020", "Maya Dewi", "Eksplorasi Laparotomi", "1997-02-14", 9),  // 12 Nov
                ("P004848", "Nugroho Jati", "Total Knee Replacement (TKR)", "1960-04-01", 9),
                ("P007010", "Olivia Puspita", "Mastektomi Radikal", "1993-06-28", 11) // 14 Nov
        };

        // Titik awal rentang waktu (Senin, 3 November 2025, pukul 07:00)
        var startDate = new DateTime(2025, 11, 3, 7, 0, 0);
        var random = new Random();

        for (int i = 0; i < dataPairs.Length; i++)
        {
            var pair = dataPairs[i];

            // Menghitung tanggal operasi berdasarkan offset hari
            var opDate = startDate.AddDays(pair.DayOffset);

            // Menambahkan jam acak antara 07:00 - 15:00
            // Random.Next(min, max) -> min inklusif, max eksklusif
            var opHour = random.Next(7, 16);
            var opMinute = random.Next(0, 60 / 15) * 15; // Kelipatan 15 menit

            var tglJam = new DateTime(opDate.Year, opDate.Month, opDate.Day, opHour, opMinute, 0, DateTimeKind.Utc);

            schedules.Add(new Schedule
            {
                PasienId = pair.PasienId,
                PasienName = pair.PasienName,
                PasienTglLahir = DateTime.Parse(pair.TglLahir).ToUniversalTime(),
                NamaOperasi = pair.NamaOperasi,
                // Mengambil data secara acak untuk detail lainnya
                Urgenitas = urgenitas[random.Next(urgenitas.Length)],
                Durasi = durasi[random.Next(durasi.Length)],
                KamarName = kamarNames[random.Next(kamarNames.Length)],
                TglJam = tglJam,
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

    // Menggunakan DateTime untuk tanggal dan waktu
    public DateTime PasienTglLahir { get; set; }

    public string NamaOperasi { get; set; }
    public string Urgenitas { get; set; }
    public string Durasi { get; set; }
    public string KamarName { get; set; }

    // Tanggal dan Jam Operasi
    public DateTime TglJam { get; set; }

    public string DokterName { get; set; }
}