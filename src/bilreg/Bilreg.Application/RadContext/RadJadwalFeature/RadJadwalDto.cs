using System.Text.Json;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalDto(
    string JadwalRadId,
    string ScheduleNo,
    string OrderId,
    string OrderNo,
    string RegId,
    string PasienId,
    string PasienName,
    DateTime BirthDate,
    string Gender,
    string Phone,
    string Modality,
    string EquipmentId,
    string EquipmentName,
    string RoomId,
    string RoomName,
    string DoctorRadiologyId,
    string DoctorRadiologyName,
    DateTime ScheduleDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int DurationMinutes,
    int TurnaroundBufferMinutes,
    string PreparationNotes,
    string LabRequirements,
    string SafetyChecklistJson,
    bool IsArrived,
    DateTime? ArrivedAt,
    bool IsCleared,
    string ClearanceNotes,
    string ClearedUserId,
    DateTime? ClearedAt,
    int Status,
    int RescheduleCount,
    string CancellationCategory,
    string CancellationReason,
    string CreatedUserId,
    DateTime CreatedDate,
    string ModifiedUserId,
    DateTime ModifiedDate) : IRadJadwalKey
{
    private static readonly DateTime EmptyDate = RadJadwalModel.EmptyDate;

    public RadJadwalDto() : this(
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, EmptyDate, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty,
        EmptyDate, TimeSpan.Zero, TimeSpan.Zero, 0, 0,
        string.Empty, string.Empty, string.Empty,
        false, null, false, string.Empty, string.Empty, null,
        (int)RadJadwalStatusEnum.Scheduled, 0, string.Empty, string.Empty,
        string.Empty, EmptyDate, string.Empty, EmptyDate)
    {
    }

    public static RadJadwalDto FromModel(RadJadwalModel model, string phone = "")
    {
        var regId = !string.IsNullOrWhiteSpace(model.RegId) ? model.RegId : model.Patient.RegId;
        var safetyJson = JsonSerializer.Serialize(model.SafetyChecklist ?? RadSafetyChecklistType.Default);

        return new RadJadwalDto(
            JadwalRadId: model.JadwalRadId,
            ScheduleNo: model.ScheduleNo,
            OrderId: model.OrderId,
            OrderNo: model.OrderNo,
            RegId: regId,
            PasienId: model.Patient.PatientId,
            PasienName: model.Patient.PatientName,
            BirthDate: model.Patient.BirthDate,
            Gender: model.Patient.Gender,
            Phone: phone ?? string.Empty,
            Modality: model.Modality.ToString(),
            EquipmentId: model.EquipmentId,
            EquipmentName: model.EquipmentName,
            RoomId: model.RoomId,
            RoomName: model.RoomName,
            DoctorRadiologyId: model.DoctorRadiologyId,
            DoctorRadiologyName: model.DoctorRadiologyName,
            ScheduleDate: model.ScheduleDate.ToDateTime(TimeOnly.MinValue),
            StartTime: model.StartTime.ToTimeSpan(),
            EndTime: model.EndTime.ToTimeSpan(),
            DurationMinutes: model.DurationMinutes,
            TurnaroundBufferMinutes: model.TurnaroundBufferMinutes,
            PreparationNotes: model.PreparationNotes,
            LabRequirements: model.LabRequirements,
            SafetyChecklistJson: safetyJson,
            IsArrived: model.IsArrived,
            ArrivedAt: model.ArrivedAt,
            IsCleared: model.IsCleared,
            ClearanceNotes: model.ClearanceNotes,
            ClearedUserId: model.ClearedByUserId,
            ClearedAt: model.ClearedAt,
            Status: (int)model.Status,
            RescheduleCount: model.RescheduleCount,
            CancellationCategory: model.CancellationCategory?.ToString() ?? string.Empty,
            CancellationReason: model.CancellationReason,
            CreatedUserId: model.AuditTrail.Created.UserId,
            CreatedDate: model.AuditTrail.Created.Timestamp,
            ModifiedUserId: model.AuditTrail.Modified.UserId,
            ModifiedDate: model.AuditTrail.Modified.Timestamp);
    }

    public RadJadwalModel ToModel()
    {
        var age = BirthDate.Year != 3000 && CreatedDate.Year != 3000
            ? ComputeAge(BirthDate, CreatedDate)
            : 0;

        var patient = new PatientSnapshotType(
            RegId: RegId,
            PatientId: PasienId,
            PatientName: PasienName,
            BirthDate: BirthDate,
            Gender: Gender,
            AgeAtOrder: age);

        var modality = ParseModality(Modality);
        var status = Enum.IsDefined(typeof(RadJadwalStatusEnum), Status)
            ? (RadJadwalStatusEnum)Status
            : RadJadwalStatusEnum.Scheduled;

        RadCancellationCategoryEnum? cancellationCategory = null;
        if (!string.IsNullOrWhiteSpace(CancellationCategory) &&
            Enum.TryParse<RadCancellationCategoryEnum>(CancellationCategory, ignoreCase: true, out var parsedCategory))
        {
            cancellationCategory = parsedCategory;
        }

        var safetyChecklist = DeserializeSafetyChecklist(SafetyChecklistJson);

        var auditTrail = new AuditTrailType(
            new AuditInfoType(CreatedUserId, CreatedDate),
            new AuditInfoType(ModifiedUserId, ModifiedDate),
            AuditInfoType.Default);

        return new RadJadwalModel(
            jadwalRadId: JadwalRadId,
            scheduleNo: ScheduleNo,
            orderId: OrderId,
            orderNo: OrderNo,
            regId: RegId,
            patient: patient,
            modality: modality,
            equipmentId: EquipmentId,
            equipmentName: EquipmentName,
            roomId: RoomId,
            roomName: RoomName,
            doctorRadiologyId: DoctorRadiologyId,
            doctorRadiologyName: DoctorRadiologyName,
            scheduleDate: DateOnly.FromDateTime(ScheduleDate),
            startTime: ToTimeOnlySafe(StartTime),
            endTime: ToTimeOnlySafe(EndTime),
            durationMinutes: DurationMinutes,
            turnaroundBufferMinutes: TurnaroundBufferMinutes,
            preparationNotes: PreparationNotes,
            labRequirements: LabRequirements,
            safetyChecklist: safetyChecklist,
            isArrived: IsArrived,
            arrivedAt: ArrivedAt,
            isCleared: IsCleared,
            clearanceNotes: ClearanceNotes,
            clearedByUserId: ClearedUserId,
            clearedAt: ClearedAt,
            status: status,
            rescheduleCount: RescheduleCount,
            cancellationReason: CancellationReason,
            cancellationCategory: cancellationCategory,
            auditTrail: auditTrail);
    }

    private static RadModalityEnum ParseModality(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "MRI" => RadModalityEnum.MR,
            "USG" => RadModalityEnum.US,
            _ => Enum.TryParse<RadModalityEnum>(normalized, ignoreCase: true, out var parsed)
                ? parsed
                : RadModalityEnum.CT
        };
    }

    private static RadSafetyChecklistType DeserializeSafetyChecklist(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return RadSafetyChecklistType.Default;

        try
        {
            return JsonSerializer.Deserialize<RadSafetyChecklistType>(json)
                   ?? RadSafetyChecklistType.Default;
        }
        catch (JsonException)
        {
            return RadSafetyChecklistType.Default;
        }
    }

    private static TimeOnly ToTimeOnlySafe(TimeSpan span)
    {
        if (span < TimeSpan.Zero || span >= TimeSpan.FromDays(1))
            return TimeOnly.MinValue;

        return TimeOnly.FromTimeSpan(span);
    }

    private static int ComputeAge(DateTime birthDate, DateTime referenceDate)
    {
        var age = referenceDate.Year - birthDate.Year;
        if (referenceDate.Date < birthDate.Date.AddYears(age))
            age--;

        return Math.Max(0, age);
    }
}

public record RadJadwalRescheduleDto(
    string RescheduleId,
    string JadwalRadId,
    int RescheduleIndex,
    DateTime OldScheduleDate,
    TimeSpan OldStartTime,
    TimeSpan OldEndTime,
    string OldEquipmentId,
    string OldEquipmentName,
    string Reason,
    string RescheduledBy,
    DateTime RescheduledAt)
{
    public RadJadwalRescheduleDto() : this(
        string.Empty, string.Empty, 0,
        RadJadwalModel.EmptyDate, TimeSpan.Zero, TimeSpan.Zero,
        string.Empty, string.Empty, string.Empty, string.Empty, RadJadwalModel.EmptyDate)
    {
    }

    public static RadJadwalRescheduleDto CreateSnapshot(
        string rescheduleId,
        RadJadwalModel previousState,
        int rescheduleIndex,
        string reason,
        string rescheduledBy,
        DateTime rescheduledAt) => new(
        RescheduleId: rescheduleId,
        JadwalRadId: previousState.JadwalRadId,
        RescheduleIndex: rescheduleIndex,
        OldScheduleDate: previousState.ScheduleDate.ToDateTime(TimeOnly.MinValue),
        OldStartTime: previousState.StartTime.ToTimeSpan(),
        OldEndTime: previousState.EndTime.ToTimeSpan(),
        OldEquipmentId: previousState.EquipmentId,
        OldEquipmentName: previousState.EquipmentName,
        Reason: reason,
        RescheduledBy: rescheduledBy,
        RescheduledAt: rescheduledAt);
}
