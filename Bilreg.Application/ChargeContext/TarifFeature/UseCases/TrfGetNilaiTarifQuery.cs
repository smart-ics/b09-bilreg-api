using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfGetNilaiTarifQuery(string TarifId, string KelasId, string TipeTarifId) :
    IRequest<TrfGetNilaiTarifResponse>, INilaiTarifCompositKey;

public record TrfGetNilaiTarifResponse(
    string NilaiTarifId, string TarifId, string TarifName,
    TipeTarifReff TipeTarif, KelasReff Kelas, decimal Nilai,
    IEnumerable<TrfGetNilaiTarifKompResponse> ListKomponen);

public record TrfGetNilaiTarifKompResponse(string KomponenId, string KomponenName, 
    IEnumerable<string> SatTugasId, decimal Nilai);

public class NilaiTarifGetHandler : IRequestHandler<TrfGetNilaiTarifQuery, TrfGetNilaiTarifResponse>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;

    public NilaiTarifGetHandler(INilaiTarifRepo nilaiTarifRepo, 
        IKomponenRepo komponenRepo)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
    }

    public Task<TrfGetNilaiTarifResponse> Handle(TrfGetNilaiTarifQuery request, CancellationToken cancellationToken)
    {
        var nilaiTarif = _nilaiTarifRepo.LoadEntity(request)
             .Match(
                 onSome: x => x,
                 onNone: () => throw new KeyNotFoundException($"Tarif {request.TarifId} not found")
             );
        var listMasterKomp = _komponenRepo
            .ListData(nilaiTarif.ListKomponen.Select(x => x.Komponen))?.ToList() ?? [];
        var listNilaiTarifKomp = new List<TrfGetNilaiTarifKompResponse>();
        
        foreach (var item in nilaiTarif.ListKomponen)
        {
             var thisKomp = listMasterKomp.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);
             if (thisKomp is null)
                 continue;

             var newKomp = new TrfGetNilaiTarifKompResponse(item.Komponen.KomponenId, item.Komponen.KomponenName,
                 thisKomp.ListSatTugas.Select(x => x.SatTugasId), item.Nilai);
             listNilaiTarifKomp.Add(newKomp);
        }
        var result = new TrfGetNilaiTarifResponse(
            nilaiTarif.NilaiTarifId, nilaiTarif.TarifId, nilaiTarif.TarifName, 
            nilaiTarif.TipeTarif, nilaiTarif.Kelas, nilaiTarif.Nilai, listNilaiTarifKomp);
        return Task.FromResult(result);
    }
}
