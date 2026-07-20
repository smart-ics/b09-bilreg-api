using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using System.Globalization;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record JadwalPraktekHarianSaveCmd(
    string? JadwalPraktekHarianId,
    string? JadwalPraktekId,
    string TglPraktek,
    string DokterId,
    string LayananId,
    string RuangId,
    string JamMulai,
    string JamSelesai,
    int MaxPasien,
    JadwalPraktekSaveAntrianPatternCmd AntrianPattern,
    string? Catatan,
    string UserId) : IRequest<JadwalPraktekHarianSaveResponse>;

public record JadwalPraktekHarianSaveResponse(string JadwalPraktekHarianId);

public class JadwalPraktekHarianSaveHandler : IRequestHandler<JadwalPraktekHarianSaveCmd, JadwalPraktekHarianSaveResponse>
{
    private readonly IJadwalPraktekHarianRepo _harianRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IRuangRepo _ruangRepo;
    private readonly IJadwalPraktekHarianOverrideGuard _overrideGuard;
    private readonly ITglJamProvider _tglJamProvider;

    public JadwalPraktekHarianSaveHandler(
        IJadwalPraktekHarianRepo harianRepo,
        IPpaRepo ppaRepo,
        ILayananRepo layananRepo,
        IRuangRepo ruangRepo,
        IJadwalPraktekHarianOverrideGuard overrideGuard,
        ITglJamProvider? tglJamProvider = null)
    {
        _harianRepo = harianRepo;
        _ppaRepo = ppaRepo;
        _layananRepo = layananRepo;
        _ruangRepo = ruangRepo;
        _overrideGuard = overrideGuard;
        _tglJamProvider = tglJamProvider;
    }

    public Task<JadwalPraktekHarianSaveResponse> Handle(
        JadwalPraktekHarianSaveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NegativeOrZero(request.MaxPasien);
        var tglPraktek = DateOnly.Parse(request.TglPraktek);
        var jamMulai = TimeOnly.ParseExact(request.JamMulai, "HH:mm", CultureInfo.InvariantCulture);
        var jamSelesai = TimeOnly.ParseExact(request.JamSelesai, "HH:mm", CultureInfo.InvariantCulture);
        var dokterKey = PpaType.Key(request.DokterId);
        var dokter = _ppaRepo.LoadEntity(dokterKey).GetValueOrThrow("Dokter tidak ditemukan");
        var layanan = _layananRepo.LoadEntity(LayananType.Key(request.LayananId))
            .GetValueOrThrow("Layanan tidak ditemukan");
        var ruang = _ruangRepo.LoadEntity(new RuangType(request.RuangId, "-", "-"))
            .GetValueOrThrow("Ruang tidak ditemukan");

        _overrideGuard.EnsureNoOperationalConflict(tglPraktek, dokterKey, jamMulai);

        var patternItems = request.AntrianPattern.Pttrn
            .Select(x => new AntrianPatternItemType(x.Desc, x.Qty));
        var antrianPattern = new AntrianPatternType(
            request.AntrianPattern.Tipe, request.AntrianPattern.Max,
            request.AntrianPattern.Rsrvd, patternItems);

        JadwalPraktekHarianType model;
        var occurredAt = _tglJamProvider.Now;
        if (!string.IsNullOrWhiteSpace(request.JadwalPraktekHarianId))
        {
            model = _harianRepo.LoadEntity(JadwalPraktekHarianType.Key(request.JadwalPraktekHarianId))
                .GetValueOrThrow("Jadwal harian tidak ditemukan");
            model = model.ApplyManualOverride(
                dokter.ToReff(), layanan.ToReff(), ruang,
                jamMulai, jamSelesai, request.MaxPasien, antrianPattern,
                request.Catatan, request.UserId, occurredAt);
        }
        else
        {
            model = JadwalPraktekHarianType.Create(
                request.JadwalPraktekId, tglPraktek, dokter, layanan, ruang,
                jamMulai, jamSelesai, request.MaxPasien, antrianPattern,
                JadwalPraktekHarianSource.MANUAL, request.Catatan, request.UserId, occurredAt);
        }

        _harianRepo.SaveChanges(model);
        return Task.FromResult(new JadwalPraktekHarianSaveResponse(model.JadwalPraktekHarianId));
    }
}
