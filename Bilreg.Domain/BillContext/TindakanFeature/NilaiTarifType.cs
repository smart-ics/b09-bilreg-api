using Bilreg.Domain.BillContext.BedUsageFeature;

namespace Bilreg.Domain.BillContext.TindakanFeature;

public record NilaiTarifType : ITarifKey
{
    private readonly List<NilaiTarifKomponenType> _listKomponen;
    public NilaiTarifType(string tarifId, string tarifName, TipeTarifReff tipeTarif, KelasReff kelas,
        IEnumerable<NilaiTarifKomponenType> listKomponen)
    {
        TarifId = tarifId;
        TarifName = tarifName;
        TipeTarif = tipeTarif;
        Kelas = kelas;
        _listKomponen = listKomponen?.ToList() ?? [];
    }
    
    public static NilaiTarifType Default 
        => new NilaiTarifType("-", "-", TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(),[]); 

    public string TarifId { get; init; }
    public string TarifName  { get; init; }
    public TipeTarifReff TipeTarif  { get; init; }
    public KelasReff Kelas { get; init; }
    public decimal Nilai => _listKomponen.Sum(x => x.Nilai);
    public IEnumerable<NilaiTarifKomponenType> ListKomponen => _listKomponen;
}

public record NilaiTarifKomponenType(int NoUrut, KomponenReff Komponen, decimal Nilai)
{
    public static NilaiTarifKomponenType Default => new NilaiTarifKomponenType(0, KomponenType.Default.ToReff(), 0);
}
    