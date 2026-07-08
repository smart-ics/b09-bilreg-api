using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmCreateOpnameRequestCmd(
    string PasienId,
    string DokterId,
    string ClinicalNotes,
    string UserId) : IRequest<AdmCreateOpnameRequestResponse>;

public record AdmCreateOpnameRequestResponse(string OpnameRequestId);

public class AdmCreateOpnameRequestHandler : IRequestHandler<AdmCreateOpnameRequestCmd, AdmCreateOpnameRequestResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IPatientAdministrationGateway _patientGateway;
    private readonly IDoctorServiceGateway _doctorGateway;
    private readonly IAuditRepo _auditRepo;

    public AdmCreateOpnameRequestHandler(
        IOpnameRequestRepo opnameRequestRepo,
        IPatientAdministrationGateway patientGateway,
        IDoctorServiceGateway doctorGateway,
        IAuditRepo auditRepo)
    {
        _opnameRequestRepo = opnameRequestRepo;
        _patientGateway = patientGateway;
        _doctorGateway = doctorGateway;
        _auditRepo = auditRepo;
    }

    public Task<AdmCreateOpnameRequestResponse> Handle(
        AdmCreateOpnameRequestCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var pasien = _patientGateway.ResolvePatient(request.PasienId);
        var dokter = _doctorGateway.ResolveDoctor(request.DokterId);

        var opnameRequest = OpnameRequestModel.Create(
            pasien,
            dokter,
            request.ClinicalNotes ?? "",
            request.UserId);

        _opnameRequestRepo.SaveChanges(opnameRequest);

        _auditRepo.SaveChanges(AuditLog.Create(
            opnameRequest.AuditTrail.Created,
            "CREATE",
            nameof(OpnameRequestModel),
            opnameRequest.OpnameRequestId));

        return Task.FromResult(new AdmCreateOpnameRequestResponse(opnameRequest.OpnameRequestId));
    }
}
