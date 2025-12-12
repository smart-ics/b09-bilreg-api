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
        var existing = _listKomponen.FirstOrDefault(x => x.Komponen.KomponenId == komponen.KomponenId);
        if (existing is not null)
            _listKomponen.Remove(existing);

        var newKomponen = new TindakanKomponenTarifModel(
            komponen.ToReff(), ppa.ToReff(),
            existing?.NoUrut ?? 1,
            qty, nilai);

        _listKomponen.Add(newKomponen);
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


