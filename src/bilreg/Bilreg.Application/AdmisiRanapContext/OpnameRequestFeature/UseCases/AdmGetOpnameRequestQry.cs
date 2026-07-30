using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

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
    string EmrOrderId,
    DateTime CrtDate,
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
            opname.EmrOrderId,
            opname.AuditTrail.Created.Timestamp,
            insurance));
    }
}
