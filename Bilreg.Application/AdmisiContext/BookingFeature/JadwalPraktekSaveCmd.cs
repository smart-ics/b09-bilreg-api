using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekSaveCmd(string JadwalPraktekId, string DokterId,
    string LayananId, int Hari,
    string JamMulai, string JamSelesai, int MaxPasien) :
    IRequest<JadwalPraktekSaveResponse>, IJadwalPraktekKey, ILayananKey;

public record JadwalPraktekSaveResponse(string JadwalPraktekId);

public class JadwalPraktekSaveHandler : IRequestHandler<JadwalPraktekSaveCmd, JadwalPraktekSaveResponse>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;
    private readonly IJadwalPraktekFactory _jadwalNunaFactory;
    private readonly IPpaRepo _petugasRepo;
    private readonly ILayananRepo _layananRepo;
    public JadwalPraktekSaveHandler(IJadwalPraktekRepo jadwalRepo,
        IJadwalPraktekFactory jadwalNunaFactory,
        IPpaRepo petugasRepo,
        ILayananRepo layananRepo)
    {
        _jadwalRepo = jadwalRepo;
        _jadwalNunaFactory = jadwalNunaFactory;
        _petugasRepo = petugasRepo;
        _layananRepo = layananRepo;
    }

    public Task<JadwalPraktekSaveResponse> Handle(JadwalPraktekSaveCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NegativeOrZero(request.MaxPasien, nameof(request.MaxPasien));
        var dokterKey = PpaType.Key(request.DokterId);
        var dokter = _petugasRepo.LoadEntity(dokterKey)
            .GetValueOrThrow("Dokter tidak ditemukan");
        var layananKey = LayananType.Key(request.LayananId);
        var layanan = _layananRepo.LoadEntity(layananKey)
            .GetValueOrThrow("Layanan tidak ditemukan");
        if (!Enum.IsDefined(typeof(DayOfWeek), request.Hari))
            throw new Exception("Hari tidak valid");
        
        var (jamMulai, jamSelesai) = ParseJam(request);
        ValidateJam(jamMulai, jamSelesai);

        // BUILD
        var listJadwal = _jadwalRepo.ListData(dokterKey)?.ToList() ?? [];
        var jadwalDb = _jadwalRepo.LoadEntity(request)
            .GetValueOrDefault(JadwalPraktekType.Default);

        JadwalPraktekType jadwal;

        if (jadwalDb.JadwalPraktekId == "-")
            jadwal = CreateNewJadwal(request, dokter, layanan, jamMulai, jamSelesai);
        else
            jadwal = UpdateJadwal(request, dokter, layanan, jadwalDb, jamMulai, jamSelesai);

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
        PpaType dokter, LayananType layanan,
        TimeOnly jamMulai, TimeOnly jamSelesai)
    {
        return _jadwalNunaFactory.Create(
            dokter,
            layanan,
            (DayOfWeek)request.Hari,
            jamMulai,
            jamSelesai,
            request.MaxPasien);
    }
    private JadwalPraktekType UpdateJadwal(
        JadwalPraktekSaveCmd request, PpaType dokter,
        LayananType layanan, JadwalPraktekType jadwalDb,
        TimeOnly jamMulai, TimeOnly jamSelesai)
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
            (DayOfWeek)request.Hari,
            jamMulai,
            jamSelesai,
            request.MaxPasien);
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

