using Ardalis.GuardClauses;
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
    bool IsDiskonDokter = false,
    decimal DiskonDokter = 0
);

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

public class TindakanGetHandler : IRequestHandler<TindakanGetQuery, TindakanGetResponse>
{
    private readonly ITindakanRepo _tdkRepo;

    public TindakanGetHandler(ITindakanRepo tdkRepo)
    {
        _tdkRepo = tdkRepo;
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
            tdk.IsDiskonDokter,
            tdk.DiskonDokter
        );

        return Task.FromResult(result);
    }
}
