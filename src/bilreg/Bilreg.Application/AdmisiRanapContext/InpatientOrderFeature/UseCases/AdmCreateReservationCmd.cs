using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmCreateReservationCmd(
    string PasienId,
    string PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest<AdmCreateReservationResponse>;

public record AdmCreateReservationResponse(string ReservationId)
{
    public string InpatientOrderId => ReservationId;
}

public class AdmCreateReservationHandler : IRequestHandler<AdmCreateReservationCmd, AdmCreateReservationResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;
    private readonly IPatientAdministrationGateway _patientGateway;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IPasienTrackerInpatientCoordinator _trackerCoordinator;

    public AdmCreateReservationHandler(
        IInpatientOrderRepo orderRepo,
        IPatientAdministrationGateway patientGateway,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        IPasienTrackerInpatientCoordinator trackerCoordinator)
    {
        _orderRepo = orderRepo;
        _patientGateway = patientGateway;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _trackerCoordinator = trackerCoordinator;
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

        var reservation = InpatientOrderModel.CreateBooking(
            pasien,
            plannedDate,
            kelas,
            bangsal,
            request.UserId,
            createdAt: occurredAt);

        _orderRepo.SaveChanges(reservation);

        _trackerCoordinator.RecordReservationCreated(reservation, occurredAt);

        _auditRepo.SaveChanges(AuditLog.Create(
            reservation.AuditTrail.Created,
            "CREATE",
            nameof(InpatientOrderModel),
            reservation.InpatientOrderId));

        return Task.FromResult(new AdmCreateReservationResponse(reservation.InpatientOrderId));
    }
}
