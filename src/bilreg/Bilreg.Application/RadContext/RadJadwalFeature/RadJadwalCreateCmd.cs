using Ardalis.GuardClauses;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalCreateCmd(
    string OrderId,
    string EquipmentId,
    DateOnly ScheduleDate,
    TimeOnly StartTime,
    string UserId,
    int? DurationMinutes = null,
    int? TurnaroundBufferMinutes = null,
    string DoctorRadiologyId = "",
    string DoctorRadiologyName = "",
    string PreparationNotes = "",
    string LabRequirements = "",
    RadSafetyChecklistType? SafetyChecklist = null) : IRequest<RadJadwalDto>;

public class RadJadwalSlotConflictException : InvalidOperationException
{
    public const string Code = "RAD_JADWAL_SLOT_CONFLICT";

    public RadJadwalSlotConflictException(string message) : base(message)
    {
    }
}

public class RadJadwalCreateHandler : IRequestHandler<RadJadwalCreateCmd, RadJadwalDto>
{
    private readonly IRadJadwalRepo _radJadwalRepo;
    private readonly IRadJadwalDal _radJadwalDal;
    private readonly IRadEquipmentDal _radEquipmentDal;
    private readonly IRadOrderRepo _radOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRadTrackerIntegration _radTrackerIntegration;

    public RadJadwalCreateHandler(
        IRadJadwalRepo radJadwalRepo,
        IRadJadwalDal radJadwalDal,
        IRadEquipmentDal radEquipmentDal,
        IRadOrderRepo radOrderRepo,
        ISequencer sequencer,
        ITglJamProvider tglJamProvider,
        IRadTrackerIntegration radTrackerIntegration)
    {
        _radJadwalRepo = radJadwalRepo;
        _radJadwalDal = radJadwalDal;
        _radEquipmentDal = radEquipmentDal;
        _radOrderRepo = radOrderRepo;
        _sequencer = sequencer;
        _tglJamProvider = tglJamProvider;
        _radTrackerIntegration = radTrackerIntegration;
    }

    public async Task<RadJadwalDto> Handle(RadJadwalCreateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.EquipmentId, nameof(request.EquipmentId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _radOrderRepo.LoadEntity(new RadOrderKey(request.OrderId))
            .Match(
                onSome: x => x,
                onNone: () => null as RadOrderModel);

        if (order is null)
            throw new KeyNotFoundException($"Order radiologi '{request.OrderId}' tidak ditemukan.");

        if (order.AuditTrail.IsVoided || order.RadOrderStatus == RadOrderStatusEnum.Cancelled)
            throw new InvalidOperationException($"Order radiologi '{request.OrderId}' sudah dibatalkan atau void.");

        if (order.RadOrderStatus != RadOrderStatusEnum.Requested)
            throw new InvalidOperationException(
                $"Order radiologi '{request.OrderId}' berstatus {order.RadOrderStatus}; penjadwalan hanya diizinkan untuk status Requested.");

        var equipment = RadJadwalCommandSupport.ValidateActiveMatchingEquipment(
            _radEquipmentDal,
            request.EquipmentId,
            order.Modality);

        var durationMinutes = request.DurationMinutes
            ?? RadJadwalCommandSupport.GetDefaultDurationMinutes(order.Modality, order.Items.Count);
        if (durationMinutes <= 0)
            throw new ArgumentException("Durasi pemeriksaan harus lebih dari 0 menit.", nameof(request.DurationMinutes));

        var bufferMinutes = request.TurnaroundBufferMinutes
            ?? RadJadwalModel.GetDefaultTurnaroundBufferMinutes(order.Modality);
        if (bufferMinutes < 0)
            throw new ArgumentException("Turnaround buffer tidak boleh negatif.", nameof(request.TurnaroundBufferMinutes));

        var totalSpan = request.StartTime.ToTimeSpan().Add(TimeSpan.FromMinutes(durationMinutes + bufferMinutes));
        if (totalSpan >= TimeSpan.FromDays(1))
            throw new ArgumentException(
                "Rentang waktu pemeriksaan melewati batas hari yang sama.",
                nameof(request.DurationMinutes));

        var endTime = TimeOnly.FromTimeSpan(totalSpan);

        var now = _tglJamProvider.Now;
        var scheduleNo = _sequencer.GetNextNo(RadJadwalModel.ScheduleNoPrefix, now);
        var audit = new AuditInfoType(request.UserId, now);

        var jadwal = RadJadwalModel.Create(
            orderId: order.OrderId,
            orderNo: order.OrderNo,
            patient: order.Patient,
            modality: order.Modality,
            scheduleNo: scheduleNo,
            equipmentId: equipment.EquipmentId,
            equipmentName: equipment.EquipmentName,
            roomId: equipment.RoomId,
            roomName: equipment.RoomName,
            scheduleDate: request.ScheduleDate,
            startTime: request.StartTime,
            durationMinutes: durationMinutes,
            audit: audit,
            turnaroundBufferMinutes: bufferMinutes,
            doctorRadiologyId: request.DoctorRadiologyId,
            doctorRadiologyName: request.DoctorRadiologyName,
            preparationNotes: request.PreparationNotes,
            labRequirements: request.LabRequirements,
            safetyChecklist: request.SafetyChecklist);

        order.Schedule(request.UserId, now);

        using (var trans = TransHelper.NewScope())
        {
            RadJadwalCommandSupport.EnsureSlotAvailable(
                _radJadwalDal,
                equipment.EquipmentId,
                equipment.EquipmentName,
                request.ScheduleDate,
                request.StartTime,
                endTime);

            _radJadwalRepo.SaveChanges(jadwal);
            _radOrderRepo.SaveChanges(order);
            trans.Complete();
        }

        await RadJadwalCommandSupport.TryPublishMilestoneAsync(
            _radTrackerIntegration,
            jadwal.RegId,
            jadwal.JadwalRadId,
            "Scheduled",
            $"Jadwal {jadwal.ScheduleNo} pada {jadwal.EquipmentName} ({jadwal.ScheduleDate:yyyy-MM-dd} {jadwal.StartTime:HH:mm})",
            cancellationToken);

        return RadJadwalDto.FromModel(jadwal);
    }
}

internal sealed record RadOrderKey(string OrderId) : IRadOrderKey;

internal static class RadJadwalCommandSupport
{
    public static RadEquipmentDto ValidateActiveMatchingEquipment(
        IRadEquipmentDal equipmentDal,
        string equipmentId,
        RadModalityEnum expectedModality)
    {
        var equipment = equipmentDal.GetData(equipmentId);
        if (equipment is null)
            throw new KeyNotFoundException($"Mesin radiologi '{equipmentId}' tidak ditemukan.");

        if (!equipment.IsActive)
            throw new InvalidOperationException(
                $"Mesin radiologi '{equipment.EquipmentId}' ({equipment.EquipmentName}) sedang tidak aktif.");

        if (!TryParseModality(equipment.Modality, out var equipmentModality) || equipmentModality != expectedModality)
            throw new InvalidOperationException(
                $"Modalitas mesin '{equipment.EquipmentId}' ({equipment.Modality}) tidak sesuai dengan modalitas '{expectedModality}'.");

        return equipment;
    }

    public static void EnsureSlotAvailable(
        IRadJadwalDal jadwalDal,
        string equipmentId,
        string equipmentName,
        DateOnly scheduleDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? excludeJadwalId = null)
    {
        var isOverlapping = jadwalDal.IsSlotOverlapping(
            equipmentId,
            scheduleDate.ToDateTime(TimeOnly.MinValue),
            startTime.ToTimeSpan(),
            endTime.ToTimeSpan(),
            excludeJadwalId);

        if (isOverlapping)
            throw new RadJadwalSlotConflictException(
                $"Slot waktu {scheduleDate:yyyy-MM-dd} {startTime:HH:mm}-{endTime:HH:mm} pada mesin '{equipmentName}' ({equipmentId}) sudah terisi.");
    }

    public static async Task TryPublishMilestoneAsync(
        IRadTrackerIntegration trackerIntegration,
        string regId,
        string jadwalRadId,
        string milestone,
        string note,
        CancellationToken ct)
    {
        try
        {
            await trackerIntegration.PublishMilestoneAsync(regId, jadwalRadId, milestone, note, ct);
        }
        catch
        {
            // Resilient: tracker failure must not block scheduling transactions.
        }
    }

    public static int GetDefaultDurationMinutes(RadModalityEnum modality, int itemCount)
    {
        var count = Math.Max(1, itemCount);
        var perItemMinutes = modality switch
        {
            RadModalityEnum.MR => 30,
            RadModalityEnum.CT => 20,
            RadModalityEnum.US => 20,
            _ => 15
        };
        return count * perItemMinutes;
    }

    public static bool TryParseModality(string value, out RadModalityEnum modality)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized == "MRI")
        {
            modality = RadModalityEnum.MR;
            return true;
        }

        if (normalized == "USG")
        {
            modality = RadModalityEnum.US;
            return true;
        }

        return Enum.TryParse(normalized, ignoreCase: true, out modality);
    }
}
