using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ReturJualFeature.UseCases;

public record ReturJualGetQuery(string ReturJualId)
    : IRequest<ReturJualGetResponse>, IReturJualKey;

public record ReturJualGetResponse(
    string ReturJualId,
    PenjualanReff Penjualan,
    LayananReff Layanan,
    string Reason,
    TipeJaminanReff TipeJaminan,
    TipeBrgReff TipeBrg,
    NilaiReturJualType Nilai,
    bool IsVoided,
    DateTime CrtDate,
    string CrtUserId,
    IEnumerable<ReturJualItemGetResponse> ListItem);

public record ReturJualItemGetResponse(
    string ReturJualItemId,
    int NoUrut,
    BrgReff Brg,
    SatuanType Satuan,
    decimal QtyJual,
    decimal QtyRetur,
    NilaiItemReturType Nilai,
    bool IsVoided);

public class ReturJualGetHandler : IRequestHandler<ReturJualGetQuery, ReturJualGetResponse>
{
    private readonly IReturJualRepo _returJualRepo;

    public ReturJualGetHandler(IReturJualRepo returJualRepo)
    {
        _returJualRepo = returJualRepo;
    }

    public Task<ReturJualGetResponse> Handle(
        ReturJualGetQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReturJualId);

        var retur = _returJualRepo.LoadEntity(request)
            .GetValueOrThrow($"Retur Jual '{request.ReturJualId}' tidak ditemukan.");

        var listItem = retur.ListItem
            .Select(x => new ReturJualItemGetResponse(
                x.ReturJualItemId,
                x.NoUrut,
                x.Brg,
                x.Satuan,
                x.QtyJual,
                x.QtyRetur,
                x.Nilai,
                x.IsVoided))
            .ToList();

        return Task.FromResult(new ReturJualGetResponse(
            retur.ReturJualId,
            retur.Penjualan,
            retur.Layanan,
            retur.Reason,
            retur.TipeJaminan,
            retur.TipeBrg,
            retur.Nilai,
            retur.AuditTrail.IsVoided,
            retur.AuditTrail.Created.Timestamp,
            retur.AuditTrail.Created.UserId,
            listItem));
    }
}