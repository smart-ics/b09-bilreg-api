using Ardalis.GuardClauses;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TindakanGetQuery(string TindakanId) : IRequest<TindakanGetResponse>, ITindakanKey;

public record TindakanGetResponse(
    string TindakanId,
    DateTime TindakanDate,
    string OrderTindakanId,
    RegReff Reg,
    LayananReff Layanan,
    KelasReff Kelas,
    TipeTarifReff TipeTarif,
    TarifReff Tarif,
    int Qty,
    decimal Total,
    bool IsVoid,
    AuditTrailType AuditTrail,
    IEnumerable<TindakanGetKomponenResponse> ListKomponen,
    string PakaiBedId = "",
    bool IsDiskonDokter = false,
    decimal DiskonDokter = 0,
    bool IsChargeBarang = false,
    IEnumerable<TindakanGetBrgResponse>? ListBarang = null
)
{
    private readonly IEnumerable<TindakanGetBrgResponse>? _listBarang = ListBarang;
    public IEnumerable<TindakanGetBrgResponse> ListBarang
    {
        get => _listBarang ?? [];
        init => _listBarang = value;
    }
}

public record TindakanGetKomponenResponse(
    string KomponenId,
    string KomponenName,
    decimal Nilai,
    int Qty,
    decimal SubTotal,
    string PpaId,
    string PpaName,
    decimal Diskon = 0
);

public record TindakanGetBrgResponse(
    string BrgId,
    string BrgName,
    string SatuanBomId,
    decimal QtyBom,
    decimal QtyKecilDeducted,
    decimal QtyPending,
    string PakaiBrgId,
    string TrsBillingId
);

public class TindakanGetHandler : IRequestHandler<TindakanGetQuery, TindakanGetResponse>
{
    private readonly ITindakanRepo _tdkRepo;
    private readonly ITindakanBarangRepo? _tdkBarangRepo;

    public TindakanGetHandler(ITindakanRepo tdkRepo) : this(tdkRepo, null!)
    {
    }

    public TindakanGetHandler(ITindakanRepo tdkRepo, ITindakanBarangRepo tdkBarangRepo)
    {
        _tdkRepo = tdkRepo;
        _tdkBarangRepo = tdkBarangRepo;
    }

    public Task<TindakanGetResponse> Handle(TindakanGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TindakanId);

        var tdk = _tdkRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tindakan {request.TindakanId} not found")
            );

        var listKomponen = tdk.ListKomponen.Select(item =>
        {
            var (ppaId, ppaName) = item is TindakanKomponenWithPpaType ppaKomp
                ? (ppaKomp.Ppa.PpaId, ppaKomp.Ppa.PpaName)
                : (string.Empty, string.Empty);

            return new TindakanGetKomponenResponse(
                item.Komponen.KomponenId,
                item.Komponen.KomponenName,
                item.Nilai,
                item.Qty,
                item.SubTotal,
                ppaId,
                ppaName,
                item.Diskon
            );
        }).ToList();

        var rawBarang = _tdkBarangRepo?.ListByTindakanId(request.TindakanId)?.Where(b => !b.IsVoid).ToList() ?? [];
        var isChargeBarang = rawBarang.Any(b => b.IsChargeBarang);
        var listBarang = rawBarang.Select(b => new TindakanGetBrgResponse(
            b.BrgId,
            b.BrgName,
            b.SatuanBomId,
            b.QtyBom,
            b.QtyKecilDeducted,
            b.QtyPending,
            b.PakaiBrgId,
            b.TrsBillingId
        )).ToList();

        var result = new TindakanGetResponse(
            tdk.TindakanId,
            tdk.TindakanDate,
            tdk.OrderTindakanId,
            tdk.Reg,
            tdk.Layanan,
            tdk.Kelas,
            tdk.TipeTarif,
            tdk.Tarif,
            tdk.Qty,
            tdk.Total,
            tdk.AuditTrail.IsVoided,
            tdk.AuditTrail,
            listKomponen,
            tdk.PakaiBedId,
            tdk.IsDiskonDokter,
            tdk.DiskonDokter,
            isChargeBarang,
            listBarang
        );

        return Task.FromResult(result);
    }
}
