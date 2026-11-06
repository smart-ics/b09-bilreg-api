using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmCreateOpnameRequestCmd(
    string PasienId,
    string DokterId,
    string PlannedDate,
    string ClinicalNotes,
    string ExternalReferenceId,
    string UserId,
    string ExternalSystemSource = "EMR_RJ",
    OrderSourceEnum OrderSource = OrderSourceEnum.EmrRawatJalan) : IRequest<AdmCreateOpnameRequestResponse>
{
    public string TrsOrderId => ExternalReferenceId;
}

public record AdmCreateOpnameRequestResponse(string OpnameRequestId, string TrsOrderId)
{
    public string InpatientOrderId => OpnameRequestId;
    public string ExternalReferenceId => TrsOrderId;
}

public class AdmCreateOpnameRequestHandler : IRequestHandler<AdmCreateOpnameRequestCmd, AdmCreateOpnameRequestResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;
    private readonly IPatientAdministrationGateway _patientGateway;
    private readonly IDoctorServiceGateway _doctorGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IPasienTrackerInpatientCoordinator _trackerCoordinator;

    public AdmCreateOpnameRequestHandler(
        IInpatientOrderRepo orderRepo,
        IPatientAdministrationGateway patientGateway,
        IDoctorServiceGateway doctorGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        IPasienTrackerInpatientCoordinator trackerCoordinator)
    {
        _orderRepo = orderRepo;
        _patientGateway = patientGateway;
        _doctorGateway = doctorGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _trackerCoordinator = trackerCoordinator;
    }

    public Task<AdmCreateOpnameRequestResponse> Handle(
        AdmCreateOpnameRequestCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.PlannedDate);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.ExternalReferenceId);

        var pasien = _patientGateway.ResolvePatient(request.PasienId);
        var dokter = _doctorGateway.ResolveDoctor(request.DokterId);
        var plannedDate = request.PlannedDate.ToDate("yyyy-MM-dd");
        var occurredAt = _tglJamProvider.Now;

        var orderSource = request.OrderSource == OrderSourceEnum.EmrIgd
            ? OrderSourceEnum.EmrIgd
            : OrderSourceEnum.EmrRawatJalan;

        var extSource = !string.IsNullOrWhiteSpace(request.ExternalSystemSource)
            ? request.ExternalSystemSource
            : (orderSource == OrderSourceEnum.EmrIgd ? "EMR_IGD" : "EMR_RJ");

        var order = InpatientOrderModel.CreateEmr(
            orderSource,
            pasien,
            dokter,
            plannedDate,
            request.ClinicalNotes ?? "",
            request.UserId,
            request.ExternalReferenceId,
            extSource,
            InpatientOrderInsuranceModel.Default,
            occurredAt);

        _orderRepo.SaveChanges(order);

        _trackerCoordinator.RecordOpnameRequestCreated(order, occurredAt);

        _auditRepo.SaveChanges(AuditLog.Create(
            order.AuditTrail.Created,
            "CREATE",
            nameof(InpatientOrderModel),
            order.InpatientOrderId));

        return Task.FromResult(new AdmCreateOpnameRequestResponse(order.InpatientOrderId, order.ExternalReferenceId));
    }
}
