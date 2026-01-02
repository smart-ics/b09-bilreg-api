using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record NilaiTarifType : INilaiTarifKey, INilaiTarifCompositKey, INilaiTarifVariant
{
    private readonly List< NilaiTarifKomponenType> _listKomponen;
    public NilaiTarifType(string nilaiTarifId, 
        string tarifId, string tarifName, 
        TipeTarifReff tipeTarif, KelasReff kelas, decimal nilai,
        IEnumerable<NilaiTarifKomponenType> listKomponen)
    {
        NilaiTarifId = nilaiTarifId;
        TarifId = tarifId;
        TarifName = tarifName;
        TipeTarif = tipeTarif;
        Kelas = kelas;
        Nilai = nilai;
        _listKomponen = listKomponen?.ToList() ?? [];
    }
    public static NilaiTarifType Default => new("-", "-", "-",
        TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 
        0, []);
    public static INilaiTarifKey Key(string id) => new NilaiTarifType(id, "", "", 
        TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 
        0, []);

    public static INilaiTarifCompositKey KeyComposite(string tarifId, string tipeTarifId, string kelasId)
    {
        var tipeTarif = new TipeTarifReff(tipeTarifId, "-");
        var kelas = new KelasReff(kelasId, "-");
        
        return new NilaiTarifType("-", tarifId, "",
            tipeTarif, kelas, 0, []);
         
    }
    public string NilaiTarifId { get; init; }
    public string TarifId { get; init; }
    public string TarifName  { get; init; }
    public string TipeTarifId => TipeTarif.TipeTarifId;
    public string KelasId => Kelas.KelasId;
    public TipeTarifReff TipeTarif { get; init; }
    public KelasReff Kelas { get; init; }
    public decimal Nilai { get; init; }
    
    public IEnumerable<NilaiTarifKomponenType> ListKomponen => _listKomponen;
}

public record NilaiTarifKomponenType(int NoUrut, KomponenReff Komponen, decimal Nilai)
{
    public static NilaiTarifKomponenType Default => new NilaiTarifKomponenType(0, KomponenType.Default.ToReff(), 0);
}

public interface INilaiTarifKey
{
    string NilaiTarifId { get;  }
}

public interface INilaiTarifCompositKey: ITarifKey, ITipeTarifKey, IKelasKey
{
}

public interface INilaiTarifVariant : ITipeTarifKey, IKelasKey
{
}