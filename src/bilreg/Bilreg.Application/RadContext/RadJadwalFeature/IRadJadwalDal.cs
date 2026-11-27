using Bilreg.Domain.RadContext.RadJadwalFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public interface IRadJadwalDal :
    IInsert<RadJadwalDto>,
    IUpdate<RadJadwalDto>
{
    RadJadwalDto? GetData(IRadJadwalKey key);
    RadJadwalDto? GetActiveByOrderId(string orderId);
    IEnumerable<RadJadwalDto> ListByOrderId(string orderId);
    IEnumerable<RadJadwalDto> ListByDateRange(
        DateTime startDate,
        DateTime endDate,
        string? modality = null,
        int? status = null,
        string? equipmentId = null);
    IEnumerable<RadJadwalCalendarSlotDto> ListCalendarSlots(
        DateTime scheduleDate,
        string? modality = null,
        string? equipmentId = null);
    bool IsSlotOverlapping(
        string equipmentId,
        DateTime scheduleDate,
        TimeSpan startTime,
        TimeSpan endTime,
        string? excludeJadwalId = null);
    void InsertReschedule(RadJadwalRescheduleDto dto);
    IEnumerable<RadJadwalRescheduleDto> ListRescheduleHistory(string jadwalRadId);
}
