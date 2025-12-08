using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanTarifModel
{
    private readonly List<TindakanKomponenTarifModel> _listKomponen;
    #region CREATION
    public TindakanTarifModel(TarifType tarif,
        IEnumerable<TindakanKomponenTarifModel> listKomponen)
    {
        Tarif = tarif.ToReff(); // <-- Validate dulu tarifnya
        _listKomponen = listKomponen.ToList() ?? [];
    }
    public static TindakanTarifModel Default => new TindakanTarifModel(
        TarifType.Default, []);
    #endregion

    public TarifReff Tarif { get; init; }
    public decimal Total => _listKomponen.Sum(t => t.Nilai);
    public IEnumerable<TindakanKomponenTarifModel> ListKomponen => _listKomponen;
}

public record TindakanKomponenTarifModel(
    KomponenReff  Komponen,
    PpaReff Ppa,
    int NoUrut,
    decimal Qty,
    decimal Nilai
);
