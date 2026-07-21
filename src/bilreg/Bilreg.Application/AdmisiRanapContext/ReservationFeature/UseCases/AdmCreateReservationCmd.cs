using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmCreateReservationCmd(
    string PasienId,
    string PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest<AdmCreateReservationResponse>;

public record AdmCreateReservationResponse(string ReservationId);

public class AdmCreateReservationHandler : IRequestHandler<AdmCreateReservationCmd, AdmCreateReservationResponse>
{
    private readonly IReservationRepo _reservationRepo;
    private readonly IPatientAdministrationGateway _patientGateway;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmCreateReservationHandler(
        IReservationRepo reservationRepo,
        IPatientAdministrationGateway patientGateway,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _reservationRepo = reservationRepo;
        _patientGateway = patientGateway;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<AdmCreateReservationResponse> Handle(
        AdmCreateReservationCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.PlannedDate);

        var pasien = _patientGateway.ResolvePatient(request.PasienId);
        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);
        var plannedDate = request.PlannedDate.ToDate("yyyy-MM-dd");
        var occurredAt = _tglJamProvider.Now;
        var reservation = ReservationModel.Create(
            pasien,
            plannedDate,
            kelas,
            bangsal,
            request.UserId,
            occurredAt);

        _reservationRepo.SaveChanges(reservation);

        _auditRepo.SaveChanges(AuditLog.Create(
            reservation.AuditTrail.Created,
            "CREATE",
            nameof(ReservationModel),
            reservation.ReservationId));

        return Task.FromResult(new AdmCreateReservationResponse(reservation.ReservationId));
    }
}
