using Ardalis.GuardClauses;
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
        Guard.Against.Null(tarif);

        Tarif = tarif.ToReff(); // <-- Validate dulu tarifnya
        _listKomponen = listKomponen.ToList() ?? [];
    }
    public static TindakanTarifModel Default => new TindakanTarifModel(
        TarifType.Default, []);
    #endregion

    public TarifReff Tarif { get; init; }
    public decimal Total => _listKomponen.Sum(t => t.Nilai);
    public IEnumerable<TindakanKomponenTarifModel> ListKomponen => _listKomponen;

    #region BEHAVIOR
    public void SetKomponen(KomponenType komponen, PpaType ppa, decimal qty, decimal nilai)
    {
        var oldKomponen = ListKomponen.FirstOrDefault(x => x.Komponen.KomponenId == komponen.KomponenId)
            ?? new TindakanKomponenTarifModel(KomponenType.Default.ToReff(), 
                PpaType.Default.ToReff(), 1, 0, 0);
        var newKomponen = new TindakanKomponenTarifModel(komponen.ToReff(), ppa.ToReff(), oldKomponen.NoUrut, qty, nilai);
        ListKomponen.ToList().Remove(oldKomponen);
        ListKomponen.ToList().Add(newKomponen);
    }
    #endregion
}

public record TindakanKomponenTarifModel(
    KomponenReff Komponen,
    PpaReff Ppa,
    int NoUrut,
    decimal Qty,
    decimal Nilai
);


