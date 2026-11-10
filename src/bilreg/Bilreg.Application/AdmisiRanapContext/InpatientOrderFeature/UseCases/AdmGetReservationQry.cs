using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmGetReservationQry(string ReservationId)
    : IRequest<AdmGetReservationResponse>, IInpatientOrderKey
{
    public string InpatientOrderId => ReservationId;
}

public record AdmGetReservationResponse(
    string ReservationId,
    ReservationStatusEnum ReservationStatus,
    PasienReff Pasien,
    DateTime PlannedDate,
    KelasReff KelasRawat,
    BangsalReff Bangsal,
    string RealizedRegId,
    DateTime CrtDate)
{
    public string InpatientOrderId => ReservationId;
}

public class AdmGetReservationHandler : IRequestHandler<AdmGetReservationQry, AdmGetReservationResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;

    public AdmGetReservationHandler(IInpatientOrderRepo orderRepo) =>
        _orderRepo = orderRepo;

    public Task<AdmGetReservationResponse> Handle(
        AdmGetReservationQry request,
        CancellationToken cancellationToken)
    {
        var order = _orderRepo.GetByKey(request.ReservationId)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");

        var resStatus = order.OrderStatus switch
        {
            InpatientOrderStatusEnum.Requested => ReservationStatusEnum.Reserved,
            InpatientOrderStatusEnum.Maintained => ReservationStatusEnum.Maintained,
            InpatientOrderStatusEnum.Fulfilled => ReservationStatusEnum.Realized,
            InpatientOrderStatusEnum.Cancelled => ReservationStatusEnum.Cancelled,
            _ => ReservationStatusEnum.Reserved
        };

        return Task.FromResult(new AdmGetReservationResponse(
            order.InpatientOrderId,
            resStatus,
            order.Pasien,
            order.PlannedDate,
            order.KelasRawat,
            order.Bangsal,
            order.FulfilledRegId,
            order.AuditTrail.Created.Timestamp));
    }
}
