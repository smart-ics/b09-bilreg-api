using Bilreg.Domain.RadContext.RadJadwalFeature;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalCalendarSlotDto(
    string JadwalRadId,
    string ScheduleNo,
    string OrderId,
    string OrderNo,
    string RegId,
    string PasienId,
    string PasienName,
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
    int Status,
    bool IsArrived,
    bool IsCleared,
    int RescheduleCount)
{
    public RadJadwalCalendarSlotDto() : this(
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty, string.Empty,
        RadJadwalModel.EmptyDate, TimeSpan.Zero, TimeSpan.Zero, 0, 0,
        (int)RadJadwalStatusEnum.Scheduled, false, false, 0)
    {
    }
}

public record RadJadwalCalendarEquipmentDto(
    string EquipmentId,
    string EquipmentName,
    string Modality,
    string RoomId,
    string RoomName,
    bool IsActive,
    IReadOnlyList<RadJadwalCalendarSlotDto> Slots);

public record RadJadwalCalendarDto(
    DateTime ScheduleDate,
    string Modality,
    IReadOnlyList<RadJadwalCalendarEquipmentDto> Equipments);
