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
    public decimal Total => _listKomponen.Sum(t => t.SubTotal);
    public IEnumerable<TindakanKomponenTarifModel> ListKomponen => _listKomponen;

    #region BEHAVIOR
    public void SetKomponen(KomponenType komponen, PpaType ppa, decimal qty, decimal nilai)
    {
        var existing = _listKomponen
        .FirstOrDefault(x => x.Komponen.KomponenId == komponen.KomponenId);

        var noUrut = existing?.NoUrut
            ?? (_listKomponen.Count == 0
                ? 1
                : _listKomponen.Max(x => x.NoUrut) + 1);

        if (existing is not null)
            _listKomponen.Remove(existing);

        var subTotal = qty * nilai;

        _listKomponen.Add(new TindakanKomponenTarifModel(
            komponen.ToReff(),
            ppa.ToReff(),
            noUrut,
            nilai,
            qty,
            subTotal));
    }

    #endregion
}

public record TindakanKomponenTarifModel(
    KomponenReff Komponen,
    PpaReff Ppa,
    int NoUrut,
    decimal Nilai,
    decimal Qty,
    decimal SubTotal
    
);


