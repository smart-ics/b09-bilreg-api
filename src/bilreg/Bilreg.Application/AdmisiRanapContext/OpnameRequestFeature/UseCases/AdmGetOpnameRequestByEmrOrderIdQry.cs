using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmGetOpnameRequestByEmrOrderIdQry(string EmrOrderId) :
    IRequest<AdmGetOpnameRequestByEmrOrderIdResponse>;

public record AdmGetOpnameRequestByEmrOrderIdResponse(
    string OpnameRequestId,
    OpnameRequestStatusEnum OpnameRequestStatus,
    PasienReff Pasien,
    string DokterId,
    string DokterName,
    string ClinicalNotes,
    string FulfilledRegId,
    string EmrOrderId,
    DateTime CrtDate,
    AdmGetOpnameRequestByEmrOrderIdInsuranceResponse Insurance);

public record AdmGetOpnameRequestByEmrOrderIdInsuranceResponse(
    TipeJaminanReff TipeJaminan, string ReffId);

public class AdmGetOpnameRequestByEmrOrderIdHandler : 
    IRequestHandler<AdmGetOpnameRequestByEmrOrderIdQry, AdmGetOpnameRequestByEmrOrderIdResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;

    public AdmGetOpnameRequestByEmrOrderIdHandler(IOpnameRequestRepo opnameRequestRepo)
    {
        _opnameRequestRepo = opnameRequestRepo;
    }

    public Task<AdmGetOpnameRequestByEmrOrderIdResponse> Handle(AdmGetOpnameRequestByEmrOrderIdQry request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.EmrOrderId);

        var opname = _opnameRequestRepo.GetByEmrOrder(request.EmrOrderId)
            .GetValueOrThrow($"Opname Request '{request.EmrOrderId}' tidak ditemukan");

        var insurance = new AdmGetOpnameRequestByEmrOrderIdInsuranceResponse(opname.Insurance.TipeJaminan, opname.Insurance.ReffId);
        return Task.FromResult(new AdmGetOpnameRequestByEmrOrderIdResponse(
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