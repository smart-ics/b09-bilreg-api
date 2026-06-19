using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Param;
using MediatR;
using System.Globalization;
using System.Net.Http.Headers;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekSaveCmd(string JadwalPraktekId, string DokterId,
    string LayananId, string RuangId, int Hari,
    string JamMulai, string JamSelesai, int MaxPasien, JadwalPraktekSaveAntrianPatternCmd AntrianPattern) :
    IRequest<JadwalPraktekSaveResponse>, IJadwalPraktekKey, ILayananKey, IRuangKey;

public record JadwalPraktekSaveAntrianPatternCmd(string Tipe, int Max, 
    int Rsrvd, IEnumerable<JadwalPraktekSaveAntrianPatternItemCmd> Pttrn);
public record JadwalPraktekSaveAntrianPatternItemCmd(string Desc, int Qty);

public record JadwalPraktekSaveResponse(string JadwalPraktekId);

public class JadwalPraktekSaveHandler : IRequestHandler<JadwalPraktekSaveCmd, JadwalPraktekSaveResponse>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;
    private readonly IJadwalPraktekFactory _jadwalNunaFactory;
    private readonly IPpaRepo _petugasRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IRuangRepo _ruangRepo;
    private readonly IGetKodeRsService _getKodeRsService;
    private readonly IJadwalPraktekSendToHfisService _jadwalSendToHfisService;
    public JadwalPraktekSaveHandler(IJadwalPraktekRepo jadwalRepo,
        IJadwalPraktekFactory jadwalNunaFactory,
        IPpaRepo petugasRepo,
        ILayananRepo layananRepo,
        IRuangRepo ruangRepo,
        IJadwalPraktekSendToHfisService jadwalSendToHfisService,
        IGetKodeRsService getKodeRsService)
    {
        _jadwalRepo = jadwalRepo;
        _jadwalNunaFactory = jadwalNunaFactory;
        _petugasRepo = petugasRepo;
        _layananRepo = layananRepo;
        _ruangRepo = ruangRepo;
        _jadwalSendToHfisService = jadwalSendToHfisService;
        _getKodeRsService = getKodeRsService;
    }

    public Task<JadwalPraktekSaveResponse> Handle(JadwalPraktekSaveCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NegativeOrZero(request.MaxPasien, nameof(request.MaxPasien));
        Guard.Against.NullOrWhiteSpace(request.RuangId, nameof(request.RuangId));
        var dokterKey = PpaType.Key(request.DokterId);
        var dokter = _petugasRepo.LoadEntity(dokterKey)
            .GetValueOrThrow("Dokter tidak ditemukan");
        var layananKey = LayananType.Key(request.LayananId);
        var layanan = _layananRepo.LoadEntity(layananKey)
            .GetValueOrThrow("Layanan tidak ditemukan");
        var ruang = _ruangRepo.LoadEntity(request).
            GetValueOrThrow($"Ruang {request.RuangId} invalid");
        if (!Enum.IsDefined(typeof(DayOfWeek), request.Hari))
            throw new Exception("Hari tidak valid");
        
        var (jamMulai, jamSelesai) = ParseJam(request);
        ValidateJam(jamMulai, jamSelesai);

        // BUILD
        var listJadwal = _jadwalRepo.ListData(dokterKey)?.ToList() ?? [];
        var jadwalDb = _jadwalRepo.LoadEntity(request)
            .GetValueOrDefault(JadwalPraktekType.Default);
        
        var antrianPattrenItem = request.AntrianPattern.
            Pttrn.Select(x => new AntrianPatternItemType(x.Desc, x.Qty));
        var antrianPattern = new AntrianPatternType(
            request.AntrianPattern.Tipe, request.AntrianPattern.Max,
            request.AntrianPattern.Rsrvd, antrianPattrenItem);

        JadwalPraktekType jadwal;

        if (jadwalDb.JadwalPraktekId == "-")
            jadwal = CreateNewJadwal(request, dokter, layanan, ruang, jamMulai, jamSelesai, antrianPattern);
        else
            jadwal = UpdateJadwal(request, dokter, layanan, ruang, jadwalDb, jamMulai, jamSelesai, antrianPattern);

        ValidateOverlap(jadwal, listJadwal);
        var jadwalHfis = BuildPayloadHfis(jadwal);

        //  WRITE
        _jadwalRepo.SaveChanges(jadwal);
        _jadwalSendToHfisService.Execute(jadwalHfis);

        return Task.FromResult(new JadwalPraktekSaveResponse(jadwal.JadwalPraktekId));
    }

    #region PRIVATE-HELPER
    private (TimeOnly, TimeOnly) ParseJam(JadwalPraktekSaveCmd request)
    {
        try
        {
            return (
                TimeOnly.ParseExact(request.JamMulai, "HH:mm", CultureInfo.InvariantCulture),
                TimeOnly.ParseExact(request.JamSelesai, "HH:mm", CultureInfo.InvariantCulture)
            );
        }
        catch
        {
            throw new Exception("Format jam harus HH:mm");
        }
    }

    private void ValidateJam(TimeOnly jamMulai, TimeOnly jamSelesai)
    {
        if (jamMulai >= jamSelesai)
            throw new Exception("Jam Mulai harus lebih kecil dari Jam Selesai");
    }

    private JadwalPraktekType CreateNewJadwal(
        JadwalPraktekSaveCmd request,
        PpaType dokter, LayananType layanan, RuangType ruang,
        TimeOnly jamMulai, TimeOnly jamSelesai, AntrianPatternType antrianPattern)
    {
        return _jadwalNunaFactory.Create(
            dokter,
            layanan,
            ruang,
            (DayOfWeek)request.Hari,
            jamMulai,
            jamSelesai,
            request.MaxPasien,
            antrianPattern
            );
    }
    private JadwalPraktekType UpdateJadwal(
        JadwalPraktekSaveCmd request, PpaType dokter,
        LayananType layanan, RuangType ruang, JadwalPraktekType jadwalDb,
        TimeOnly jamMulai, TimeOnly jamSelesai, AntrianPatternType antrianPattern)
    {
        var lynDkReff = new LayananDkReff(
            layanan.LayananDk.LayananDkId,
            layanan.LayananDk.LayananDkName);

        return new JadwalPraktekType(
            request.JadwalPraktekId,
            dokter.ToReff(),
            layanan.ToReff(),
            lynDkReff,
            dokter.GroupSpesialis,
            ruang,
            (DayOfWeek)request.Hari,
            jamMulai,
            jamSelesai,
            request.MaxPasien,
            antrianPattern);
    }
    
    private void ValidateOverlap(
        JadwalPraktekType jadwal,
        List<JadwalPraktekType> listJadwal)
    {
        var isOverlap = listJadwal
            .Where(x => x.Hari == jadwal.Hari &&
                        x.Dokter.PpaId == jadwal.Dokter.PpaId &&
                        x.JadwalPraktekId != jadwal.JadwalPraktekId)
            .Any(x =>
                jadwal.JamMulai < x.JamSelesai &&
                jadwal.JamSelesai > x.JamMulai
            );

        if (isOverlap)
            throw new Exception("Jadwal beririsan");
    }


    // Build Payload toHfis
    private JadwalPraktekSendHfisPayload BuildPayloadHfis(JadwalPraktekType jadwal)
    {
        int limitBpjs = jadwal.MaxPasien;
        int limitAll = jadwal.MaxPasien;

        if (jadwal.AntrianPattern.Tipe == "FLAG")
        {
            var maxPasien = jadwal.MaxPasien;
            var bpjsPart = jadwal.AntrianPattern.Pttrn.FirstOrDefault(x => x.Desc == "BPJS")?.Qty ?? 0;
            var umumPart = jadwal.AntrianPattern.Pttrn.FirstOrDefault(x => x.Desc == "UMUM")?.Qty ?? 0;
            var totalRasio = bpjsPart + umumPart;

            limitBpjs = totalRasio > 0
                ? (int)Math.Round((double)(bpjsPart * maxPasien) / totalRasio)
                : maxPasien;

            limitAll = maxPasien;
        }
        
        var rsid = _getKodeRsService.Execute() ?? string.Empty;
        var hari = (int)jadwal.Hari;

        var result = new JadwalPraktekSendHfisPayload(
            rsid,
            new[]
            {
            new ItemJadwalPraktekHfis(
                jadwal.Dokter.PpaId,
                hari,
                jadwal.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
                jadwal.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture),
                limitAll,
                limitBpjs)
            });

        return result;
    }
    #endregion
}

