using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

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
    AdmGetOpnameRequestByTrsOrderIdInsuranceResponse Insurance)
{
    public string InpatientOrderId => OpnameRequestId;
    public string ExternalReferenceId => TrsOrderId;
}

public record AdmGetOpnameRequestByTrsOrderIdInsuranceResponse(
    TipeJaminanReff TipeJaminan, string ReffId);

public class AdmGetOpnameRequestByTrsOrderIdHandler :
    IRequestHandler<AdmGetOpnameRequestByTrsOrderIdQry, AdmGetOpnameRequestByTrsOrderIdResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;

    public AdmGetOpnameRequestByTrsOrderIdHandler(IInpatientOrderRepo orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public Task<AdmGetOpnameRequestByTrsOrderIdResponse> Handle(
        AdmGetOpnameRequestByTrsOrderIdQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsOrderId);

        var order = _orderRepo.GetByExternalReference(request.TrsOrderId, "EMR_RJ")
            .Match(
                onSome: x => x,
                onNone: () => _orderRepo.GetByExternalReference(request.TrsOrderId, "EMR_IGD")
                    .Match(
                        onSome: y => y,
                        onNone: () => _orderRepo.GetByExternalReference(request.TrsOrderId, "")
                            .GetValueOrThrow($"Opname Request '{request.TrsOrderId}' tidak ditemukan")));

        var opnameStatus = order.OrderStatus switch
        {
            InpatientOrderStatusEnum.Fulfilled => OpnameRequestStatusEnum.Fulfilled,
            InpatientOrderStatusEnum.Cancelled => OpnameRequestStatusEnum.Cancelled,
            _ => OpnameRequestStatusEnum.Requested
        };

        var insurance = new AdmGetOpnameRequestByTrsOrderIdInsuranceResponse(
            order.Insurance.TipeJaminan,
            order.Insurance.JaminanReffId);

        return Task.FromResult(new AdmGetOpnameRequestByTrsOrderIdResponse(
            order.InpatientOrderId,
            opnameStatus,
            order.Pasien,
            order.Dokter.PpaId,
            order.Dokter.PpaName,
            order.ClinicalNotes,
            order.FulfilledRegId,
            order.ExternalReferenceId,
            order.AuditTrail.Created.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            insurance));
    }
}
