using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmGetOpnameRequestQry(string OpnameRequestId)
    : IRequest<AdmGetOpnameRequestResponse>, IOpnameRequestKey;

public record AdmGetOpnameRequestResponse(
    string OpnameRequestId,
    OpnameRequestStatusEnum OpnameRequestStatus,
    PasienReff Pasien,
    string DokterId,
    string DokterName,
    string ClinicalNotes,
    string FulfilledRegId,
    DateTime CrtDate);

public class AdmGetOpnameRequestHandler : IRequestHandler<AdmGetOpnameRequestQry, AdmGetOpnameRequestResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;

    public AdmGetOpnameRequestHandler(IOpnameRequestRepo opnameRequestRepo) =>
        _opnameRequestRepo = opnameRequestRepo;

    public Task<AdmGetOpnameRequestResponse> Handle(
        AdmGetOpnameRequestQry request,
        CancellationToken cancellationToken)
    {
        var opname = _opnameRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");

        return Task.FromResult(new AdmGetOpnameRequestResponse(
            opname.OpnameRequestId,
            opname.OpnameRequestStatus,
            opname.Pasien,
            opname.Dokter.PpaId,
            opname.Dokter.PpaName,
            opname.ClinicalNotes,
            opname.FulfilledRegId,
            opname.AuditTrail.Created.Timestamp));
    }
}
