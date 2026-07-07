using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmGetReservationQry(string ReservationId)
    : IRequest<AdmGetReservationResponse>, IReservationKey;

public record AdmGetReservationResponse(
    string ReservationId,
    ReservationStatusEnum ReservationStatus,
    PasienReff Pasien,
    DateTime PlannedDate,
    KelasReff KelasRawat,
    BangsalReff Bangsal,
    string RealizedRegId,
    DateTime CrtDate);

public class AdmGetReservationHandler : IRequestHandler<AdmGetReservationQry, AdmGetReservationResponse>
{
    private readonly IReservationRepo _reservationRepo;

    public AdmGetReservationHandler(IReservationRepo reservationRepo) =>
        _reservationRepo = reservationRepo;

    public Task<AdmGetReservationResponse> Handle(
        AdmGetReservationQry request,
        CancellationToken cancellationToken)
    {
        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");

        return Task.FromResult(new AdmGetReservationResponse(
            reservation.ReservationId,
            reservation.ReservationStatus,
            reservation.Pasien,
            reservation.PlannedDate,
            reservation.KelasRawat,
            reservation.Bangsal,
            reservation.RealizedRegId,
            reservation.AuditTrail.Created.Timestamp));
    }
}
