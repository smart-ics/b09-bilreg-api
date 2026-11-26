using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalCalendarTimelineQuery(
    DateTime ScheduleDate,
    string? Modality = null,
    string? EquipmentId = null) : IRequest<RadJadwalCalendarDto>;

public class RadJadwalCalendarTimelineHandler : IRequestHandler<RadJadwalCalendarTimelineQuery, RadJadwalCalendarDto>
{
    private readonly IRadJadwalDal _jadwalDal;
    private readonly IRadEquipmentDal _equipmentDal;

    public RadJadwalCalendarTimelineHandler(IRadJadwalDal jadwalDal, IRadEquipmentDal equipmentDal)
    {
        _jadwalDal = jadwalDal;
        _equipmentDal = equipmentDal;
    }

    public Task<RadJadwalCalendarDto> Handle(
        RadJadwalCalendarTimelineQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var targetDate = request.ScheduleDate == default ? DateTime.Today : request.ScheduleDate.Date;
        var modality = string.IsNullOrWhiteSpace(request.Modality) ? null : request.Modality.Trim();
        var equipmentId = string.IsNullOrWhiteSpace(request.EquipmentId) ? null : request.EquipmentId.Trim();

        var equipments = LoadEquipments(modality, equipmentId);
        var slots = _jadwalDal
            .ListCalendarSlots(targetDate, modality, equipmentId)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.ScheduleNo)
            .ToList();

        var slotsByEquipment = slots
            .GroupBy(x => x.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<RadJadwalCalendarSlotDto>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var timelineEquipments = new List<RadJadwalCalendarEquipmentDto>();
        var includedEquipmentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var equipment in equipments)
        {
            includedEquipmentIds.Add(equipment.EquipmentId);
            var equipmentSlots = slotsByEquipment.TryGetValue(equipment.EquipmentId, out var foundSlots)
                ? foundSlots
                : Array.Empty<RadJadwalCalendarSlotDto>();

            timelineEquipments.Add(new RadJadwalCalendarEquipmentDto(
                EquipmentId: equipment.EquipmentId,
                EquipmentName: equipment.EquipmentName,
                Modality: equipment.Modality,
                RoomId: equipment.RoomId,
                RoomName: equipment.RoomName,
                IsActive: equipment.IsActive,
                Slots: equipmentSlots));
        }

        // Preserve slots even if their equipment is no longer active in master lookup.
        foreach (var group in slotsByEquipment)
        {
            if (includedEquipmentIds.Contains(group.Key) || group.Value.Count == 0)
                continue;

            var sample = group.Value[0];
            timelineEquipments.Add(new RadJadwalCalendarEquipmentDto(
                EquipmentId: sample.EquipmentId,
                EquipmentName: sample.EquipmentName,
                Modality: sample.Modality,
                RoomId: sample.RoomId,
                RoomName: sample.RoomName,
                IsActive: false,
                Slots: group.Value));
        }

        var result = new RadJadwalCalendarDto(
            ScheduleDate: targetDate,
            Modality: modality ?? string.Empty,
            Equipments: timelineEquipments);

        return Task.FromResult(result);
    }

    private IReadOnlyList<RadEquipmentDto> LoadEquipments(string? modality, string? equipmentId)
    {
        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            var single = _equipmentDal.GetData(equipmentId);
            if (single is null)
                return Array.Empty<RadEquipmentDto>();

            if (!string.IsNullOrWhiteSpace(modality) &&
                !string.Equals(single.Modality, modality, StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<RadEquipmentDto>();
            }

            return new[] { single };
        }

        var list = !string.IsNullOrWhiteSpace(modality)
            ? _equipmentDal.ListByModality(modality, activeOnly: true)
            : _equipmentDal.ListActive();

        return list
            .OrderBy(x => x.Modality)
            .ThenBy(x => x.EquipmentId)
            .ToList();
    }
}
