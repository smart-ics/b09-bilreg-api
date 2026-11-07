using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmGetOpnameRequestQry(string OpnameRequestId)
    : IRequest<AdmGetOpnameRequestResponse>, IInpatientOrderKey
{
    public string InpatientOrderId => OpnameRequestId;
}

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
    AdmGetOpnameRequestInsuranceResponse Insurance)
{
    public string InpatientOrderId => OpnameRequestId;
    public string ExternalReferenceId => TrsOrderId;
}

public record AdmGetOpnameRequestInsuranceResponse(
    TipeJaminanReff TipeJaminan, string ReffId);

public class AdmGetOpnameRequestHandler : IRequestHandler<AdmGetOpnameRequestQry, AdmGetOpnameRequestResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;

    public AdmGetOpnameRequestHandler(IInpatientOrderRepo orderRepo) =>
        _orderRepo = orderRepo;

    public Task<AdmGetOpnameRequestResponse> Handle(
        AdmGetOpnameRequestQry request,
        CancellationToken cancellationToken)
    {
        var order = _orderRepo.GetByKey(request.OpnameRequestId)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");

        var opnameStatus = order.OrderStatus switch
        {
            InpatientOrderStatusEnum.Fulfilled => OpnameRequestStatusEnum.Fulfilled,
            InpatientOrderStatusEnum.Cancelled => OpnameRequestStatusEnum.Cancelled,
            _ => OpnameRequestStatusEnum.Requested
        };

        var insurance = new AdmGetOpnameRequestInsuranceResponse(
            order.Insurance.TipeJaminan,
            order.Insurance.JaminanReffId);

        return Task.FromResult(new AdmGetOpnameRequestResponse(
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
