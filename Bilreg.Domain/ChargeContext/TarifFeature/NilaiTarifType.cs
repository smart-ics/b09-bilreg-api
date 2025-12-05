using System.Runtime.InteropServices.ComTypes;
using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record NilaiTarifType : ITarifKey
{
    private readonly List<NilaiTarifVariantType> _listVariant;
    public NilaiTarifType(string tarifId, string tarifName,
        IEnumerable<NilaiTarifVariantType> listVariant)
    {
        TarifId = tarifId;
        TarifName = tarifName;
        _listVariant = listVariant?.ToList() ?? [];
    }
    
    public static NilaiTarifType Default => new("-", "-",[]); 

    public string TarifId { get; init; }
    public string TarifName  { get; init; }
    public IEnumerable<NilaiTarifVariantType> ListVariant => _listVariant;
}


public record NilaiTarifVariantType
{
    private readonly List<NilaiTarifKomponenType> _listKomponen;

    public NilaiTarifVariantType(TipeTarifReff tipeTarif, KelasReff kelas, IEnumerable<NilaiTarifKomponenType> listKomponen)
    {
        TipeTarif = tipeTarif;
        Kelas = kelas;
        _listKomponen = listKomponen?.ToList() ?? [];
    }

    public NilaiTarifVariantType Default = new (TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), []);
    //public string VariantId => $"{TarifId}-{TipeTarif.TipeTarifId}-{Kelas.KelasId}",

    public TipeTarifReff TipeTarif { get; init; }
    public KelasReff Kelas { get; init; }
    public decimal Nilai => _listKomponen.Sum(x => x.Nilai);
    public IEnumerable<NilaiTarifKomponenType> ListKomponen => _listKomponen;
    
};
public record NilaiTarifKomponenType(int NoUrut, KomponenReff Komponen, decimal Nilai)
{
    public static NilaiTarifKomponenType Default => new NilaiTarifKomponenType(0, KomponenType.Default.ToReff(), 0);
}
