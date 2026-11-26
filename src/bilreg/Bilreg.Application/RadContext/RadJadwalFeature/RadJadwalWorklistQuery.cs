using Ardalis.GuardClauses;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using MediatR;

namespace Bilreg.Application.RadContext.RadJadwalFeature;

public record RadJadwalWorklistQuery(
    DateTime StartDate,
    DateTime EndDate,
    string? Modality = null,
    int? Status = null,
    string? EquipmentId = null) : IRequest<RadJadwalWorklistResponse>;

public record RadJadwalWorklistResponse(
    IReadOnlyList<RadJadwalDto> Schedules,
    IReadOnlyList<RadOrderWorklistView> PendingOrders);

public class RadJadwalWorklistHandler : IRequestHandler<RadJadwalWorklistQuery, RadJadwalWorklistResponse>
{
    private readonly IRadJadwalDal _jadwalDal;
    private readonly IRadOrderDal _orderDal;

    public RadJadwalWorklistHandler(IRadJadwalDal jadwalDal, IRadOrderDal orderDal)
    {
        _jadwalDal = jadwalDal;
        _orderDal = orderDal;
    }

    public Task<RadJadwalWorklistResponse> Handle(
        RadJadwalWorklistQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var startDate = request.StartDate == default ? DateTime.Today : request.StartDate.Date;
        var endDate = request.EndDate == default ? startDate : request.EndDate.Date;

        if (endDate < startDate)
            throw new ArgumentException("EndDate tidak boleh lebih awal dari StartDate.", nameof(request));

        var modality = string.IsNullOrWhiteSpace(request.Modality) ? null : request.Modality.Trim();
        var equipmentId = string.IsNullOrWhiteSpace(request.EquipmentId) ? null : request.EquipmentId.Trim();

        var schedules = _jadwalDal
            .ListByDateRange(startDate, endDate, modality, request.Status, equipmentId)
            .OrderBy(x => x.ScheduleDate)
            .ThenBy(x => x.StartTime)
            .ToList();

        var pendingOrders = new List<RadOrderWorklistView>();
        var shouldIncludePendingOrders = !request.Status.HasValue || request.Status.Value == (int)RadJadwalStatusEnum.Scheduled;
        if (shouldIncludePendingOrders)
        {
            var seenOrderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayOrders = _orderDal.GetWorklist(
                    date,
                    modality ?? string.Empty,
                    (int)RadOrderStatusEnum.Requested);

                foreach (var order in dayOrders)
                {
                    if (seenOrderIds.Add(order.OrderId))
                        pendingOrders.Add(order);
                }
            }
        }

        return Task.FromResult(new RadJadwalWorklistResponse(schedules, pendingOrders));
    }
}
