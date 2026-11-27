using Ardalis.GuardClauses;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalClearanceCmd(
    string JadwalRadId,
    bool IsArrived,
    bool IsCleared,
    string ClearanceNotes,
    string UserId,
    RadSafetyChecklistType? SafetyChecklist = null) : IRequest<RadJadwalDto>, IRadJadwalKey;

public class RadJadwalClearanceHandler : IRequestHandler<RadJadwalClearanceCmd, RadJadwalDto>
{
    private readonly IRadJadwalRepo _radJadwalRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRadTrackerIntegration _radTrackerIntegration;

    public RadJadwalClearanceHandler(
        IRadJadwalRepo radJadwalRepo,
        ITglJamProvider tglJamProvider,
        IRadTrackerIntegration radTrackerIntegration)
    {
        _radJadwalRepo = radJadwalRepo;
        _tglJamProvider = tglJamProvider;
        _radTrackerIntegration = radTrackerIntegration;
    }

    public async Task<RadJadwalDto> Handle(RadJadwalClearanceCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.JadwalRadId, nameof(request.JadwalRadId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var jadwal = _radJadwalRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => null as RadJadwalModel);

        if (jadwal is null)
            throw new KeyNotFoundException($"Jadwal radiologi '{request.JadwalRadId}' tidak ditemukan.");

        var now = _tglJamProvider.Now;

        if (request.IsArrived && !jadwal.IsArrived)
            jadwal.RecordArrival(request.UserId, now);

        jadwal.VerifyClearance(
            request.IsCleared,
            request.ClearanceNotes ?? string.Empty,
            request.UserId,
            request.SafetyChecklist,
            now);

        var isCompleted = jadwal.IsArrived && jadwal.IsCleared;
        if (isCompleted)
            jadwal.Complete(request.UserId, now);

        _radJadwalRepo.SaveChanges(jadwal);

        if (isCompleted)
        {
            await RadJadwalCommandSupport.TryPublishMilestoneAsync(
                _radTrackerIntegration,
                jadwal.RegId,
                jadwal.JadwalRadId,
                "Completed",
                $"Jadwal {jadwal.ScheduleNo} selesai verifikasi keselamatan dan siap pemindaian.",
                cancellationToken);
        }

        return RadJadwalDto.FromModel(jadwal);
    }
}
