using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record JadwalPraktekHarianCancelCmd(
    string JadwalPraktekHarianId,
    string Catatan,
    string UserId) : IRequest;

public class JadwalPraktekHarianCancelHandler : IRequestHandler<JadwalPraktekHarianCancelCmd>
{
    private readonly IJadwalPraktekHarianRepo _harianRepo;
    private readonly IJadwalPraktekHarianOverrideGuard _overrideGuard;
    private readonly ITglJamProvider _tglJamProvider;

    public JadwalPraktekHarianCancelHandler(
        IJadwalPraktekHarianRepo harianRepo,
        IJadwalPraktekHarianOverrideGuard overrideGuard,
        ITglJamProvider? tglJamProvider = null)
    {
        _harianRepo = harianRepo;
        _overrideGuard = overrideGuard;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(JadwalPraktekHarianCancelCmd request, CancellationToken cancellationToken)
    {
        var model = _harianRepo.LoadEntity(JadwalPraktekHarianType.Key(request.JadwalPraktekHarianId))
            .GetValueOrThrow("Jadwal harian tidak ditemukan");

        _overrideGuard.EnsureNoOperationalConflict(
            model.TglPraktek, PpaType.Key(model.Dokter.PpaId), model.JamMulai);

        var cancelled = model.Cancel(request.Catatan, request.UserId, _tglJamProvider.Now);
        _harianRepo.SaveChanges(cancelled);
        return Task.CompletedTask;
    }
}
