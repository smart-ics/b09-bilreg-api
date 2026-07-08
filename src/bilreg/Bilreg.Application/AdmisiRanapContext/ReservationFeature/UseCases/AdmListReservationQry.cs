using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmListReservationQry(
    ReservationStatusEnum? Status = null,
    DateTime? PlannedFrom = null,
    DateTime? PlannedTo = null) : IRequest<AdmListReservationResponse>;

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
    private readonly IReservationRepo _reservationRepo;

    public AdmListReservationHandler(IReservationRepo reservationRepo) =>
        _reservationRepo = reservationRepo;

    public Task<AdmListReservationResponse> Handle(
        AdmListReservationQry request,
        CancellationToken cancellationToken)
    {
        var items = _reservationRepo
            .ListData(new ReservationListFilter(request.Status, request.PlannedFrom, request.PlannedTo))
            .Select(Map)
            .ToList();

        return Task.FromResult(new AdmListReservationResponse(items));
    }

    private static AdmReservationListItem Map(ReservationModel reservation) =>
        new(
            reservation.ReservationId,
            reservation.ReservationStatus,
            reservation.Pasien.PasienId,
            reservation.Pasien.PasienName,
            reservation.PlannedDate,
            reservation.KelasRawat.KelasId,
            reservation.KelasRawat.KelasName,
            reservation.Bangsal.BangsalId,
            reservation.Bangsal.BangsalName,
            reservation.AuditTrail.Created.Timestamp);
}
