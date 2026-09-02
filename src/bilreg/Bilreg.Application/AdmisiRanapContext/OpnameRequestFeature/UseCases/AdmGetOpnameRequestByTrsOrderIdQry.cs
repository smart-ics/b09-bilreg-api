using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmGetOpnameRequestByTrsOrderIdQry(string TrsOrderId) :
    IRequest<AdmGetOpnameRequestByTrsOrderIdResponse>;

public record AdmGetOpnameRequestByTrsOrderIdResponse(
    string OpnameRequestId,
    OpnameRequestStatusEnum OpnameRequestStatus,
    PasienReff Pasien,
    string DokterId,
    string DokterName,
    string ClinicalNotes,
    string FulfilledRegId,
    string TrsOrderId,
    string CrtDate,
    AdmGetOpnameRequestByTrsOrderIdInsuranceResponse Insurance);

public record AdmGetOpnameRequestByTrsOrderIdInsuranceResponse(
    TipeJaminanReff TipeJaminan, string ReffId);

public class AdmGetOpnameRequestByTrsOrderIdHandler : 
    IRequestHandler<AdmGetOpnameRequestByTrsOrderIdQry, AdmGetOpnameRequestByTrsOrderIdResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;

    public AdmGetOpnameRequestByTrsOrderIdHandler(IOpnameRequestRepo opnameRequestRepo)
    {
        _opnameRequestRepo = opnameRequestRepo;
    }

    public Task<AdmGetOpnameRequestByTrsOrderIdResponse> Handle(AdmGetOpnameRequestByTrsOrderIdQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsOrderId);

        var opname = _opnameRequestRepo.GetByTrsOrder(request.TrsOrderId)
            .GetValueOrThrow($"Opname Request '{request.TrsOrderId}' tidak ditemukan");

        var insurance = new AdmGetOpnameRequestByTrsOrderIdInsuranceResponse(opname.Insurance.TipeJaminan, opname.Insurance.ReffId);
        return Task.FromResult(new AdmGetOpnameRequestByTrsOrderIdResponse(
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