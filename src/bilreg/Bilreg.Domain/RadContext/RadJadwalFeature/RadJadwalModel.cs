using Ardalis.GuardClauses;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.RadContext.RadJadwalFeature;

public class RadJadwalModel : IRadJadwalKey
{
    public const string IdPrefix = "JAD";
    public const string ScheduleNoPrefix = "SCH";
    public static readonly DateTime EmptyDate = new(3000, 1, 1);

    #region CREATION

    public RadJadwalModel(
        string jadwalRadId,
        string scheduleNo,
        string orderId,
        string orderNo,
        string regId,
        PatientSnapshotType patient,
        RadModalityEnum modality,
        string equipmentId,
        string equipmentName,
        string roomId,
        string roomName,
        string doctorRadiologyId,
        string doctorRadiologyName,
        DateOnly scheduleDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int durationMinutes,
        int turnaroundBufferMinutes,
        string preparationNotes,
        string labRequirements,
        RadSafetyChecklistType safetyChecklist,
        bool isArrived,
        DateTime? arrivedAt,
        bool isCleared,
        string clearanceNotes,
        string clearedByUserId,
        DateTime? clearedAt,
        RadJadwalStatusEnum status,
        int rescheduleCount,
        string cancellationReason,
        RadCancellationCategoryEnum? cancellationCategory,
        AuditTrailType auditTrail)
    {
        JadwalRadId = jadwalRadId ?? string.Empty;
        ScheduleNo = scheduleNo ?? string.Empty;
        OrderId = orderId ?? string.Empty;
        OrderNo = orderNo ?? string.Empty;
        RegId = regId ?? string.Empty;
        Patient = patient ?? PatientSnapshotType.Default;
        Modality = modality;
        EquipmentId = equipmentId ?? string.Empty;
        EquipmentName = equipmentName ?? string.Empty;
        RoomId = roomId ?? string.Empty;
        RoomName = roomName ?? string.Empty;
        DoctorRadiologyId = doctorRadiologyId ?? string.Empty;
        DoctorRadiologyName = doctorRadiologyName ?? string.Empty;

        ScheduleDate = scheduleDate;
        StartTime = startTime;
        EndTime = endTime;
        DurationMinutes = durationMinutes;
        TurnaroundBufferMinutes = turnaroundBufferMinutes;

        PreparationNotes = preparationNotes ?? string.Empty;
        LabRequirements = labRequirements ?? string.Empty;
        SafetyChecklist = safetyChecklist ?? RadSafetyChecklistType.Default;
        IsArrived = isArrived;
        ArrivedAt = arrivedAt;
        IsCleared = isCleared;
        ClearanceNotes = clearanceNotes ?? string.Empty;
        ClearedByUserId = clearedByUserId ?? string.Empty;
        ClearedAt = clearedAt;

        Status = status;
        RescheduleCount = rescheduleCount;
        CancellationReason = cancellationReason ?? string.Empty;
        CancellationCategory = cancellationCategory;
        AuditTrail = auditTrail ?? AuditTrailType.Default;
    }

    public static RadJadwalModel Default => new(
        jadwalRadId: "-",
        scheduleNo: string.Empty,
        orderId: string.Empty,
        orderNo: string.Empty,
        regId: string.Empty,
        patient: PatientSnapshotType.Default,
        modality: RadModalityEnum.CT,
        equipmentId: string.Empty,
        equipmentName: string.Empty,
        roomId: string.Empty,
        roomName: string.Empty,
        doctorRadiologyId: string.Empty,
        doctorRadiologyName: string.Empty,
        scheduleDate: DateOnly.FromDateTime(EmptyDate),
        startTime: TimeOnly.MinValue,
        endTime: TimeOnly.MinValue,
        durationMinutes: 0,
        turnaroundBufferMinutes: 0,
        preparationNotes: string.Empty,
        labRequirements: string.Empty,
        safetyChecklist: RadSafetyChecklistType.Default,
        isArrived: false,
        arrivedAt: null,
        isCleared: false,
        clearanceNotes: string.Empty,
        clearedByUserId: string.Empty,
        clearedAt: null,
        status: RadJadwalStatusEnum.Scheduled,
        rescheduleCount: 0,
        cancellationReason: string.Empty,
        cancellationCategory: null,
        auditTrail: AuditTrailType.Default);

    public static int GetDefaultTurnaroundBufferMinutes(RadModalityEnum modality)
        => modality == RadModalityEnum.MR ? 15 : 10;

    public static RadJadwalModel Create(
        string orderId,
        string orderNo,
        PatientSnapshotType patient,
        RadModalityEnum modality,
        string scheduleNo,
        string equipmentId,
        string equipmentName,
        string roomId,
        string roomName,
        DateOnly scheduleDate,
        TimeOnly startTime,
        int durationMinutes,
        AuditInfoType audit,
        int? turnaroundBufferMinutes = null,
        string doctorRadiologyId = "",
        string doctorRadiologyName = "",
        string preparationNotes = "",
        string labRequirements = "",
        RadSafetyChecklistType? safetyChecklist = null,
        string jadwalRadId = "")
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.NullOrWhiteSpace(orderNo, nameof(orderNo));
        Guard.Against.Null(patient, nameof(patient));
        Guard.Against.NullOrWhiteSpace(patient.PatientName, nameof(patient.PatientName));
        Guard.Against.NullOrWhiteSpace(scheduleNo, nameof(scheduleNo));
        Guard.Against.NullOrWhiteSpace(equipmentId, nameof(equipmentId));
        Guard.Against.Null(audit, nameof(audit));
        Guard.Against.NullOrWhiteSpace(audit.UserId, nameof(audit.UserId));

        if (durationMinutes <= 0)
            throw new ArgumentException("Durasi pemeriksaan harus lebih dari 0 menit.", nameof(durationMinutes));

        var buffer = turnaroundBufferMinutes ?? GetDefaultTurnaroundBufferMinutes(modality);
        if (buffer < 0)
            throw new ArgumentException("Turnaround buffer tidak boleh negatif.", nameof(turnaroundBufferMinutes));

        var totalSpan = startTime.ToTimeSpan().Add(TimeSpan.FromMinutes(durationMinutes + buffer));
        if (totalSpan >= TimeSpan.FromDays(1))
            throw new ArgumentException(
                "Rentang waktu pemeriksaan melewati batas hari yang sama.",
                nameof(durationMinutes));

        var endTime = TimeOnly.FromTimeSpan(totalSpan);
        if (endTime <= startTime)
            throw new ArgumentException("Waktu selesai harus lebih besar dari waktu mulai.", nameof(startTime));

        var finalId = string.IsNullOrWhiteSpace(jadwalRadId) ? NunaId.New(IdPrefix) : jadwalRadId.Trim();
        var auditTrail = AuditTrailType.Create(audit.UserId, audit.Timestamp);

        return new RadJadwalModel(
            jadwalRadId: finalId,
            scheduleNo: scheduleNo.Trim(),
            orderId: orderId.Trim(),
            orderNo: orderNo.Trim(),
            regId: patient.RegId ?? string.Empty,
            patient: patient,
            modality: modality,
            equipmentId: equipmentId.Trim(),
            equipmentName: (equipmentName ?? string.Empty).Trim(),
            roomId: (roomId ?? string.Empty).Trim(),
            roomName: (roomName ?? string.Empty).Trim(),
            doctorRadiologyId: (doctorRadiologyId ?? string.Empty).Trim(),
            doctorRadiologyName: (doctorRadiologyName ?? string.Empty).Trim(),
            scheduleDate: scheduleDate,
            startTime: startTime,
            endTime: endTime,
            durationMinutes: durationMinutes,
            turnaroundBufferMinutes: buffer,
            preparationNotes: (preparationNotes ?? string.Empty).Trim(),
            labRequirements: (labRequirements ?? string.Empty).Trim(),
            safetyChecklist: safetyChecklist ?? RadSafetyChecklistType.Default,
            isArrived: false,
            arrivedAt: null,
            isCleared: false,
            clearanceNotes: string.Empty,
            clearedByUserId: string.Empty,
            clearedAt: null,
            status: RadJadwalStatusEnum.Scheduled,
            rescheduleCount: 0,
            cancellationReason: string.Empty,
            cancellationCategory: null,
            auditTrail: auditTrail);
    }

    #endregion

    #region INVARIANTS & METHODS

    public void Reschedule(
        DateOnly newScheduleDate,
        TimeOnly newStartTime,
        TimeOnly newEndTime,
        string newEquipmentId,
        string newEquipmentName,
        string newRoomId,
        string newRoomName,
        string reason,
        string userId,
        DateTime rescheduledAt = default)
    {
        Guard.Against.NullOrWhiteSpace(newEquipmentId, nameof(newEquipmentId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("Alasan penjadwalan ulang minimal 3 karakter.", nameof(reason));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Jadwal {JadwalRadId} sudah void; penjadwalan ulang tidak diizinkan.");

        if (Status != RadJadwalStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} berstatus {Status}; penjadwalan ulang hanya diizinkan untuk status Scheduled.");

        if (newEndTime <= newStartTime)
            throw new ArgumentException("Waktu selesai harus lebih besar dari waktu mulai.", nameof(newEndTime));

        var totalMinutes = (int)(newEndTime - newStartTime).TotalMinutes;
        if (totalMinutes >= TurnaroundBufferMinutes)
        {
            DurationMinutes = totalMinutes - TurnaroundBufferMinutes;
        }
        else
        {
            DurationMinutes = totalMinutes;
            TurnaroundBufferMinutes = 0;
        }

        ScheduleDate = newScheduleDate;
        StartTime = newStartTime;
        EndTime = newEndTime;
        EquipmentId = newEquipmentId.Trim();
        EquipmentName = (newEquipmentName ?? string.Empty).Trim();
        RoomId = (newRoomId ?? string.Empty).Trim();
        RoomName = (newRoomName ?? string.Empty).Trim();
        RescheduleCount += 1;

        var at = rescheduledAt == default ? DateTime.Now : rescheduledAt;
        AuditTrail.Modif(userId, at);
    }

    public void Reschedule(
        DateOnly newScheduleDate,
        TimeOnly newStartTime,
        TimeOnly newEndTime,
        string newEquipmentId,
        string reason,
        string userId,
        DateTime rescheduledAt = default)
        => Reschedule(
            newScheduleDate,
            newStartTime,
            newEndTime,
            newEquipmentId,
            EquipmentName,
            RoomId,
            RoomName,
            reason,
            userId,
            rescheduledAt);

    public void Cancel(
        RadCancellationCategoryEnum category,
        string reason,
        string userId,
        DateTime cancelledAt = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (!Enum.IsDefined(typeof(RadCancellationCategoryEnum), category))
            throw new ArgumentException("Kategori pembatalan tidak valid.", nameof(category));

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new ArgumentException("Alasan pembatalan minimal 3 karakter.", nameof(reason));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Jadwal {JadwalRadId} sudah void; pembatalan tidak diizinkan.");

        if (Status != RadJadwalStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} berstatus {Status}; pembatalan hanya diizinkan untuk status Scheduled.");

        var r = reason.Trim();
        if (r.Length > 200)
            r = r[..200];

        var at = cancelledAt == default ? DateTime.Now : cancelledAt;
        Status = RadJadwalStatusEnum.Cancelled;
        CancellationCategory = category;
        CancellationReason = r;
        AuditTrail.Modif(userId, at);
    }

    public void RecordArrival(string userId, DateTime arrivedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Jadwal {JadwalRadId} sudah void; pencatatan kedatangan tidak diizinkan.");

        if (Status != RadJadwalStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} berstatus {Status}; pencatatan kedatangan hanya diizinkan untuk status Scheduled.");

        var at = arrivedAt == default ? DateTime.Now : arrivedAt;
        IsArrived = true;
        ArrivedAt = at;
        AuditTrail.Modif(userId, at);
    }

    public void VerifyClearance(
        bool isCleared,
        string clearanceNotes,
        string clearedByUserId,
        RadSafetyChecklistType? updatedChecklist = null,
        DateTime clearedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(clearedByUserId, nameof(clearedByUserId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Jadwal {JadwalRadId} sudah void; verifikasi clearance tidak diizinkan.");

        if (Status != RadJadwalStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} berstatus {Status}; verifikasi clearance hanya diizinkan untuk status Scheduled.");

        var notes = (clearanceNotes ?? string.Empty).Trim();
        if (notes.Length > 500)
            notes = notes[..500];

        if (updatedChecklist != null)
            SafetyChecklist = updatedChecklist;

        var at = clearedAt == default ? DateTime.Now : clearedAt;
        IsCleared = isCleared;
        ClearanceNotes = notes;
        ClearedByUserId = clearedByUserId.Trim();
        ClearedAt = at;
        AuditTrail.Modif(clearedByUserId, at);
    }

    public void Complete(string userId, DateTime completedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (AuditTrail.IsVoided)
            throw new InvalidOperationException($"Jadwal {JadwalRadId} sudah void; penyelesaian jadwal tidak diizinkan.");

        if (Status != RadJadwalStatusEnum.Scheduled)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} berstatus {Status}; penyelesaian jadwal hanya diizinkan untuk status Scheduled.");

        if (!IsArrived)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} tidak dapat diselesaikan karena pasien belum tercatat tiba (IsArrived = false).");

        if (!IsCleared)
            throw new InvalidOperationException(
                $"Jadwal {JadwalRadId} tidak dapat diselesaikan karena belum lolos verifikasi keselamatan (IsCleared = false).");

        var at = completedAt == default ? DateTime.Now : completedAt;
        Status = RadJadwalStatusEnum.Completed;
        AuditTrail.Modif(userId, at);
    }

    #endregion

    #region PROPERTIES

    public string JadwalRadId { get; init; }
    public string ScheduleNo { get; init; }
    public string OrderId { get; init; }
    public string OrderNo { get; init; }
    public string RegId { get; init; }
    public PatientSnapshotType Patient { get; init; }
    public RadModalityEnum Modality { get; init; }
    public string EquipmentId { get; private set; }
    public string EquipmentName { get; private set; }
    public string RoomId { get; private set; }
    public string RoomName { get; private set; }
    public string DoctorRadiologyId { get; private set; }
    public string DoctorRadiologyName { get; private set; }

    // Waktu & Durasi
    public DateOnly ScheduleDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public int TurnaroundBufferMinutes { get; private set; }

    // Persiapan & Safety Clearance
    public string PreparationNotes { get; private set; }
    public string LabRequirements { get; private set; }
    public RadSafetyChecklistType SafetyChecklist { get; private set; }
    public bool IsArrived { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public bool IsCleared { get; private set; }
    public string ClearanceNotes { get; private set; }
    public string ClearedByUserId { get; private set; }
    public DateTime? ClearedAt { get; private set; }

    // Status Siklus Hidup & Audit Reschedule
    public RadJadwalStatusEnum Status { get; private set; }
    public int RescheduleCount { get; private set; }
    public string CancellationReason { get; private set; }
    public RadCancellationCategoryEnum? CancellationCategory { get; private set; }
    public AuditTrailType AuditTrail { get; init; }

    public bool IsScheduled => Status == RadJadwalStatusEnum.Scheduled;
    public bool IsCompleted => Status == RadJadwalStatusEnum.Completed;
    public bool IsCancelled => Status == RadJadwalStatusEnum.Cancelled;
    public bool IsActive => Status == RadJadwalStatusEnum.Scheduled && !AuditTrail.IsVoided;

    #endregion
}
