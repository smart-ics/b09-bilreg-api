using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmMaintainReservationCmd(
    string ReservationId,
    string PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IReservationKey;

public class AdmMaintainReservationHandler : IRequestHandler<AdmMaintainReservationCmd>
{
    private readonly IReservationRepo _reservationRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmMaintainReservationHandler(
        IReservationRepo reservationRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _reservationRepo = reservationRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmMaintainReservationCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");
        var snapshotJson = AuditLogSnapshotJson.Serialize(reservation);
        var plannedDate = request.PlannedDate.ToDate("yyyy-MM-dd");
        var occurredAt = _tglJamProvider.Now;
        var maintained = reservation.Maintain(plannedDate, kelas, bangsal, request.UserId, occurredAt);

        _reservationRepo.SaveChanges(maintained);

        _auditRepo.SaveChanges(AuditLog.Create(
            maintained.AuditTrail.Modified,
            "UPDATE",
            nameof(ReservationModel),
            maintained.ReservationId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}
