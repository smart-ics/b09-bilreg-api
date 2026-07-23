using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmCreateOpnameRequestCmd(
    string PasienId,
    string DokterId,
    string PlannedDate,
    string ClinicalNotes,
    string UserId) : IRequest<AdmCreateOpnameRequestResponse>;

public record AdmCreateOpnameRequestResponse(string OpnameRequestId);

public class AdmCreateOpnameRequestHandler : IRequestHandler<AdmCreateOpnameRequestCmd, AdmCreateOpnameRequestResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IPatientAdministrationGateway _patientGateway;
    private readonly IDoctorServiceGateway _doctorGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmCreateOpnameRequestHandler(
        IOpnameRequestRepo opnameRequestRepo,
        IPatientAdministrationGateway patientGateway,
        IDoctorServiceGateway doctorGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _opnameRequestRepo = opnameRequestRepo;
        _patientGateway = patientGateway;
        _doctorGateway = doctorGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<AdmCreateOpnameRequestResponse> Handle(
        AdmCreateOpnameRequestCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.PlannedDate);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var pasien = _patientGateway.ResolvePatient(request.PasienId);
        var dokter = _doctorGateway.ResolveDoctor(request.DokterId);
        var plannedDate = request.PlannedDate.ToDate("yyyy-MM-dd");
        var occurredAt = _tglJamProvider.Now;
        var opnameRequest = OpnameRequestModel.Create(
            pasien,
            dokter,
            plannedDate,
            request.ClinicalNotes ?? "",
            request.UserId,
            occurredAt);

        _opnameRequestRepo.SaveChanges(opnameRequest);

        _auditRepo.SaveChanges(AuditLog.Create(
            opnameRequest.AuditTrail.Created,
            "CREATE",
            nameof(OpnameRequestModel),
            opnameRequest.OpnameRequestId));

        return Task.FromResult(new AdmCreateOpnameRequestResponse(opnameRequest.OpnameRequestId));
    }
}
