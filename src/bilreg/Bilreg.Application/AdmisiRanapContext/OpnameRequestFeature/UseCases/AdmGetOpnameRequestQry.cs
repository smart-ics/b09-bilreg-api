using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

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
    string TrsOrderId,
    string CrtDate,
    AdmGetOpnameRequestInsuranceResponse Insurance);

public record AdmGetOpnameRequestInsuranceResponse(
    TipeJaminanReff TipeJaminan, string ReffId);
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

        var insurance = new AdmGetOpnameRequestInsuranceResponse(opname.Insurance.TipeJaminan, opname.Insurance.ReffId);
        return Task.FromResult(new AdmGetOpnameRequestResponse(
            opname.OpnameRequestId,
            opname.OpnameRequestStatus,
            opname.Pasien,
            opname.Dokter.PpaId,
            opname.Dokter.PpaName,
            opname.ClinicalNotes,
            opname.FulfilledRegId,
            opname.TrsOrderId,
            opname.AuditTrail.Created.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            insurance));
    }
}
