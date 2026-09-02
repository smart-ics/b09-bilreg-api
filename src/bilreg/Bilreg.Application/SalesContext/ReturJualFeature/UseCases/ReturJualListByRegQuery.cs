using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ReturJualFeature.UseCases;

public record ReturJualListByRegQuery(string RegId)
    : IRequest<IEnumerable<ReturJualListByRegResponse>>;

public record ReturJualListByRegResponse(
    string ReturJualId,
    PenjualanReff Penjualan,
    LayananReff Layanan,
    string Reason,
    TipeJaminanReff TipeJaminan,
    TipeBrgReff TipeBrg,
    NilaiReturJualType Nilai,
    bool IsVoided,
    DateTime CrtDate,
    string CrtUserId
);

public class ReturJualListByRegHandler
    : IRequestHandler<ReturJualListByRegQuery, IEnumerable<ReturJualListByRegResponse>>
{
    private readonly IReturJualRepo _returJualRepo;

    public ReturJualListByRegHandler(IReturJualRepo returJualRepo)
    {
        _returJualRepo = returJualRepo;
    }

    public Task<IEnumerable<ReturJualListByRegResponse>> Handle(
        ReturJualListByRegQuery request,
        CancellationToken cancellationToken)
    {
        var regKey = RegModel.Key(request.RegId);
        var listRetur = _returJualRepo.ListData(regKey);
        var response = listRetur.Select(GenResponseItem);
        return Task.FromResult(response);
    }

    private static ReturJualListByRegResponse GenResponseItem(ReturJualModel retur)
        => new(
            retur.ReturJualId,
            retur.Penjualan,
            retur.Layanan,
            retur.Reason,
            retur.TipeJaminan,
            retur.TipeBrg,
            retur.Nilai,
            retur.AuditTrail.IsVoided,
            retur.AuditTrail.Created.Timestamp,
            retur.AuditTrail.Created.UserId
        );
}