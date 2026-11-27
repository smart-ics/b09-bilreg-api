using Ardalis.GuardClauses;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalRescheduleCmd(
    string JadwalRadId,
    string NewEquipmentId,
    DateOnly NewScheduleDate,
    TimeOnly NewStartTime,
    TimeOnly NewEndTime,
    string Reason,
    string UserId) : IRequest<RadJadwalDto>, IRadJadwalKey;

public class RadJadwalRescheduleHandler : IRequestHandler<RadJadwalRescheduleCmd, RadJadwalDto>
{
    private const string RescheduleIdPrefix = "RSC";

    private readonly IRadJadwalRepo _radJadwalRepo;
    private readonly IRadJadwalDal _radJadwalDal;
    private readonly IRadEquipmentDal _radEquipmentDal;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRadTrackerIntegration _radTrackerIntegration;

    public RadJadwalRescheduleHandler(
        IRadJadwalRepo radJadwalRepo,
        IRadJadwalDal radJadwalDal,
        IRadEquipmentDal radEquipmentDal,
        ITglJamProvider tglJamProvider,
        IRadTrackerIntegration radTrackerIntegration)
    {
        _radJadwalRepo = radJadwalRepo;
        _radJadwalDal = radJadwalDal;
        _radEquipmentDal = radEquipmentDal;
        _tglJamProvider = tglJamProvider;
        _radTrackerIntegration = radTrackerIntegration;
    }

    public async Task<RadJadwalDto> Handle(RadJadwalRescheduleCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.JadwalRadId, nameof(request.JadwalRadId));
        Guard.Against.NullOrWhiteSpace(request.NewEquipmentId, nameof(request.NewEquipmentId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        if (request.NewEndTime <= request.NewStartTime)
            throw new ArgumentException("Waktu selesai harus lebih besar dari waktu mulai.", nameof(request.NewEndTime));

        var jadwal = _radJadwalRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => null as RadJadwalModel);

        if (jadwal is null)
            throw new KeyNotFoundException($"Jadwal radiologi '{request.JadwalRadId}' tidak ditemukan.");

        if (jadwal.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Jadwal {jadwal.JadwalRadId} sudah void; penjadwalan ulang tidak diizinkan.");

        if (jadwal.Status != RadJadwalStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Jadwal {jadwal.JadwalRadId} berstatus {jadwal.Status}; penjadwalan ulang hanya diizinkan untuk status Scheduled.");

        var equipment = RadJadwalCommandSupport.ValidateActiveMatchingEquipment(
            _radEquipmentDal,
            request.NewEquipmentId,
            jadwal.Modality);

        using (var trans = TransHelper.NewScope())
        {
            RadJadwalCommandSupport.EnsureSlotAvailable(
                _radJadwalDal,
                equipment.EquipmentId,
                equipment.EquipmentName,
                request.NewScheduleDate,
                request.NewStartTime,
                request.NewEndTime,
                excludeJadwalId: request.JadwalRadId);

            var now = _tglJamProvider.Now;
            var rescheduleId = NunaId.New(RescheduleIdPrefix);
            var rescheduleIndex = jadwal.RescheduleCount + 1;
            var snapshot = RadJadwalRescheduleDto.CreateSnapshot(
                rescheduleId,
                jadwal,
                rescheduleIndex,
                request.Reason.Trim(),
                request.UserId.Trim(),
                now);

            jadwal.Reschedule(
                request.NewScheduleDate,
                request.NewStartTime,
                request.NewEndTime,
                equipment.EquipmentId,
                equipment.EquipmentName,
                equipment.RoomId,
                equipment.RoomName,
                request.Reason,
                request.UserId,
                now);

            _radJadwalRepo.SaveReschedule(jadwal, snapshot);
            trans.Complete();
        }

        await RadJadwalCommandSupport.TryPublishMilestoneAsync(
            _radTrackerIntegration,
            jadwal.RegId,
            jadwal.JadwalRadId,
            "Rescheduled",
            $"Jadwal {jadwal.ScheduleNo} dipindahkan ke {jadwal.EquipmentName} ({jadwal.ScheduleDate:yyyy-MM-dd} {jadwal.StartTime:HH:mm})",
            cancellationToken);

        return RadJadwalDto.FromModel(jadwal);
    }
}
