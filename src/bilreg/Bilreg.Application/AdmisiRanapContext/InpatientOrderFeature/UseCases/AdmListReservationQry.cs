using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmListReservationQry(
    ReservationStatusEnum? Status = null,
    string? PlannedFrom = null,
    string? PlannedTo = null) : IRequest<AdmListReservationResponse>;

public record AdmListReservationResponse(IReadOnlyList<AdmReservationListItem> Items);

public record AdmReservationListItem(
    string ReservationId,
    ReservationStatusEnum ReservationStatus,
    string PasienId,
    string PasienName,
    DateTime PlannedDate,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    DateTime CrtDate);

public class AdmListReservationHandler : IRequestHandler<AdmListReservationQry, AdmListReservationResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;

    public AdmListReservationHandler(IInpatientOrderRepo orderRepo) =>
        _orderRepo = orderRepo;

    public Task<AdmListReservationResponse> Handle(
        AdmListReservationQry request,
        CancellationToken cancellationToken)
    {
        DateTime? plannedFrom = null;
        DateTime? plannedTo = null;

        if (!string.IsNullOrWhiteSpace(request.PlannedFrom))
            plannedFrom = request.PlannedFrom.ToDate("yyyy-MM-dd");

        if (!string.IsNullOrWhiteSpace(request.PlannedTo))
            plannedTo = request.PlannedTo.ToDate("yyyy-MM-dd");

        InpatientOrderStatusEnum? statusFilter = request.Status switch
        {
            ReservationStatusEnum.Reserved => InpatientOrderStatusEnum.Requested,
            ReservationStatusEnum.Maintained => InpatientOrderStatusEnum.Maintained,
            ReservationStatusEnum.Realized => InpatientOrderStatusEnum.Fulfilled,
            ReservationStatusEnum.Cancelled => InpatientOrderStatusEnum.Cancelled,
            _ => null
        };

        var filter = new InpatientOrderListFilter(
            Status: statusFilter,
            OrderSource: OrderSourceEnum.AdmisiBooking,
            PlannedFrom: plannedFrom,
            PlannedTo: plannedTo);

        var items = _orderRepo
            .ListData(filter)
            .Where(x => x.OrderSource == OrderSourceEnum.AdmisiBooking)
            .Select(Map)
            .ToList();

        return Task.FromResult(new AdmListReservationResponse(items));
    }

    private static AdmReservationListItem Map(InpatientOrderModel order)
    {
        var resStatus = order.OrderStatus switch
        {
            InpatientOrderStatusEnum.Requested => ReservationStatusEnum.Reserved,
            InpatientOrderStatusEnum.Maintained => ReservationStatusEnum.Maintained,
            InpatientOrderStatusEnum.Fulfilled => ReservationStatusEnum.Realized,
            InpatientOrderStatusEnum.Cancelled => ReservationStatusEnum.Cancelled,
            _ => ReservationStatusEnum.Reserved
        };

        return new AdmReservationListItem(
            order.InpatientOrderId,
            resStatus,
            order.Pasien.PasienId,
            order.Pasien.PasienName,
            order.PlannedDate,
            order.KelasRawat.KelasId,
            order.KelasRawat.KelasName,
            order.Bangsal.BangsalId,
            order.Bangsal.BangsalName,
            order.AuditTrail.Created.Timestamp);
    }
}
