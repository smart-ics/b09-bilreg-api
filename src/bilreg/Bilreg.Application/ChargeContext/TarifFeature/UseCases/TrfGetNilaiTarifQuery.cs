using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfGetNilaiTarifQuery(
    string TarifId,
    string TipeTarifId,
    string KelasId,
    string TipeJaminanId = "",
    string LayananId = "") :
    IRequest<TrfGetNilaiTarifResponse>, INilaiTarifCompositKey;

public record TrfGetNilaiTarifResponse(
    string NilaiTarifId,
    string TarifId,
    string TarifName,
    TipeTarifReff TipeTarif,
    KelasReff Kelas,
    decimal Nilai,
    IEnumerable<TrfGetNilaiTarifKompResponse> ListKomponen,
    bool IsChargeBarang,
    IEnumerable<TrfGetNilaiTarifBrgResponse> ListBarang)
{
    public TrfGetNilaiTarifResponse(
        string nilaiTarifId,
        string tarifId,
        string tarifName,
        TipeTarifReff tipeTarif,
        KelasReff kelas,
        decimal nilai,
        IEnumerable<TrfGetNilaiTarifKompResponse> listKomponen)
        : this(nilaiTarifId, tarifId, tarifName, tipeTarif, kelas, nilai, listKomponen, false, [])
    {
    }

    public TrfGetNilaiTarifResponse(
        string nilaiTarifId,
        string tarifId,
        string tarifName,
        TipeTarifReff tipeTarif,
        KelasReff kelas,
        decimal nilai,
        IEnumerable<TrfGetNilaiTarifKompResponse> listKomponen,
        bool isChargeBarang)
        : this(nilaiTarifId, tarifId, tarifName, tipeTarif, kelas, nilai, listKomponen, isChargeBarang, [])
    {
    }
}

public record TrfGetNilaiTarifKompResponse(
    string KomponenId,
    string KomponenName, 
    IEnumerable<string> SatTugasId,
    decimal Nilai);

public record TrfGetNilaiTarifBrgResponse(
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Qty,
    int NoUrut,
    bool IsChargeBarang,
    decimal HargaSatuan,
    decimal SubTotalPerTindakan);

public class NilaiTarifGetHandler : IRequestHandler<TrfGetNilaiTarifQuery, TrfGetNilaiTarifResponse>
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITarifBarangRepo _tarifBarangRepo;
    private readonly IBrgPricingService _brgPricingService;

    public NilaiTarifGetHandler(
        INilaiTarifRepo nilaiTarifRepo, 
        IKomponenRepo komponenRepo,
        ITarifRepo tarifRepo,
        ITarifBarangRepo tarifBarangRepo,
        IBrgPricingService brgPricingService)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _tarifRepo = tarifRepo;
        _tarifBarangRepo = tarifBarangRepo;
        _brgPricingService = brgPricingService;
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

        var isChargeBarang = _tarifRepo.LoadEntity(TarifType.Key(nilaiTarif.TarifId))
            .Match(t => t.IsChargeBarang, () => false);

        var listBarangData = _tarifBarangRepo.ListData(TarifType.Key(nilaiTarif.TarifId))?
            .OrderBy(x => x.NoUrut)
            .ToList() ?? [];

        var listNilaiTarifBrg = new List<TrfGetNilaiTarifBrgResponse>();
        var tipeJaminanId = request.TipeJaminanId ?? string.Empty;
        var layananId = request.LayananId ?? string.Empty;

        foreach (var item in listBarangData)
        {
            decimal hargaSatuan = 0m;
            decimal subTotalPerTindakan = 0m;

            if (isChargeBarang && !string.IsNullOrWhiteSpace(item.BrgId))
            {
                var priceResult = _brgPricingService.CalculateItemPrice(
                    item.BrgId,
                    item.QtyBarang,
                    tipeJaminanId,
                    layananId);
                hargaSatuan = priceResult.Nilai.Harga;
                subTotalPerTindakan = hargaSatuan * item.QtyBarang;
            }

            listNilaiTarifBrg.Add(new TrfGetNilaiTarifBrgResponse(
                item.BrgId,
                item.BrgName,
                item.SatuanId,
                item.SatuanName,
                item.QtyBarang,
                item.NoUrut,
                isChargeBarang,
                hargaSatuan,
                subTotalPerTindakan));
        }

        var result = new TrfGetNilaiTarifResponse(
            nilaiTarif.NilaiTarifId,
            nilaiTarif.TarifId,
            nilaiTarif.TarifName, 
            nilaiTarif.TipeTarif,
            nilaiTarif.Kelas,
            nilaiTarif.Nilai,
            listNilaiTarifKomp,
            isChargeBarang,
            listNilaiTarifBrg);

        return Task.FromResult(result);
    }
}
