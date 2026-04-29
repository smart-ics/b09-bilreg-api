using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using System.Globalization;

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
    public JadwalPraktekSaveHandler(IJadwalPraktekRepo jadwalRepo,
        IJadwalPraktekFactory jadwalNunaFactory,
        IPpaRepo petugasRepo,
        ILayananRepo layananRepo,
        IRuangRepo ruangRepo)
    {
        _jadwalRepo = jadwalRepo;
        _jadwalNunaFactory = jadwalNunaFactory;
        _petugasRepo = petugasRepo;
        _layananRepo = layananRepo;
        _ruangRepo = ruangRepo;
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

        //  WRITE
        _jadwalRepo.SaveChanges(jadwal);
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
    #endregion
}

