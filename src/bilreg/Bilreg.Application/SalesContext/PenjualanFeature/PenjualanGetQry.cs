using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.SalesContext.Shared;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.SalesContext.PenjualanFeature;

public record PenjualanGetQry(string PenjualanId)
    : IRequest<PenjualanGetResponse>, IPenjualanKey;

public record PenjualanGetResponse(
    string PenjualanId,
    string ResepId,
    RegReff Register,
    DokterReff Dokter,
    LayananReff Layanan,
    LayananReff LayananResep,
    TipeJaminanReff TipeJaminan,
    TipeBrgReff TipeBrg,
    NilaiPenjualanType Nilai,
    bool IsVoided,
    DateTime CrtDate,
    string CrtUserId,
    IEnumerable<PenjualanItemGetResponse> ListItem);

public record PenjualanItemGetResponse(
    string PenjualanItemId,
    int NoUrut,
    BrgReff Brg,
    SatuanType Satuan,
    decimal Qty,
    EtiketType Etiket,
    NilaiItemType Nilai,
    bool IsVoided,
    IEnumerable<PenjualanItemRacikGetResponse> ListItemRacik);

public record PenjualanItemRacikGetResponse(
    int NoUrut,
    BrgReff Brg,
    SatuanType Satuan,
    decimal Qty,
    decimal Dosis,
    string DosisTxt);

public class PenjualanGetHandler : IRequestHandler<PenjualanGetQry, PenjualanGetResponse>
{
    private readonly IPenjualanRepo _penjualanRepo;

    public PenjualanGetHandler(IPenjualanRepo penjualanRepo)
    {
        _penjualanRepo = penjualanRepo;
    }

    public Task<PenjualanGetResponse> Handle(
        PenjualanGetQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PenjualanId);

        var penjualan = _penjualanRepo.LoadEntity(request)
            .GetValueOrThrow($"Penjualan '{request.PenjualanId}' tidak ditemukan.");

        var listItem = penjualan.ListItem
            .Select(x => new PenjualanItemGetResponse(
                x.PenjualanItemId,
                x.NoUrut,
                x.Brg,
                x.Satuan,
                x.Qty,
                x.Etiket,
                x.Nilai,
                x.IsVoided,
                x.ListItemRacik.Select(r => new PenjualanItemRacikGetResponse(
                    r.NoUrut,
                    r.Brg,
                    r.Satuan,
                    r.Qty,
                    r.Dosis,
                    r.DosisTxt))))
            .ToList();

        return Task.FromResult(new PenjualanGetResponse(
            penjualan.PenjualanId,
            penjualan.ResepId,
            penjualan.Register,
            penjualan.Dokter,
            penjualan.Layanan,
            penjualan.LayananResep,
            penjualan.TipeJaminan,
            penjualan.TipeBrg,
            penjualan.Nilai,
            penjualan.AuditTrail.IsVoided,
            penjualan.AuditTrail.Created.Timestamp,
            penjualan.AuditTrail.Created.UserId,
            listItem));
    }
}
