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
    string TindakanDate,
    string PakaiBedId,
    int Qty,
    decimal Total,
    RegReff Reg,
    LayananReff Layanan,
    KelasReff Kelas,
    TipeTarifReff TipeTarif,
    TarifReff Tarif,
    IEnumerable<TindakanKomponenBase> ListKomponen,
    AuditTrailType AuditTrail,
    string OrderTindakanId = "");

public class TindakanGetHandler : IRequestHandler<TindakanGetQuery, TindakanGetResponse>
{
    private readonly ITindakanRepo _tdkRepo;

    public TindakanGetHandler(ITindakanRepo tdkRepo)
    {
        _tdkRepo = tdkRepo;
    }

    public Task<TindakanGetResponse> Handle(TindakanGetQuery request, CancellationToken cancellationToken)
    {
        var tdk = _tdkRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tindakan {request.TindakanId} not found")
            );

        var result = new TindakanGetResponse(
            tdk.TindakanId,
            tdk.TindakanDate.ToString("yyyy-MM-dd HH:mm:ss"),
            tdk.PakaiBedId,
            tdk.Qty,
            tdk.Total,
            tdk.Reg,
            tdk.Layanan,
            tdk.Kelas,
            tdk.TipeTarif,
            tdk.Tarif,
            tdk.ListKomponen,
            tdk.AuditTrail,
            tdk.OrderTindakanId);

        return Task.FromResult(result);
    }
}
