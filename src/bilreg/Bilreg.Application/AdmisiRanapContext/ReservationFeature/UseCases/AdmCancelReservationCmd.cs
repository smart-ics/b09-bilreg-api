using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmCancelReservationCmd(string ReservationId, string UserId) : IRequest, IReservationKey;

public class AdmCancelReservationHandler : IRequestHandler<AdmCancelReservationCmd>
{
    private readonly IReservationRepo _reservationRepo;
    private readonly IAuditRepo _auditRepo;
    public AdmCancelReservationHandler(IReservationRepo reservationRepo, 
        IAuditRepo auditRepo)
    {
        _reservationRepo = reservationRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(AdmCancelReservationCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation {request.ReservationId} not found");
        var snapshotJson = AuditLogSnapshotJson.Serialize(reservation);
        var cancelled = reservation.Cancel(request.UserId);

        _reservationRepo.SaveChanges(cancelled);
        _auditRepo.SaveChanges(AuditLog.Create(
            cancelled.AuditTrail.Voided,
            "VOID",
            nameof(ReservationModel),
            cancelled.ReservationId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}
