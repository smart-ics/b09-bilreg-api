using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RekapCetakSub;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public record KarcisType : IKarcisKey
{
    private readonly List<KarcisKomponenType> _listKomponen;
    private readonly List<LayananReff> _listLayanan;
    
    #region CREATION
    public KarcisType(string karcisId, string karcisName, bool isAktif, 
        InstalasiDkType instalasiDk, RekapCetakReff rekapCetak, TarifReff defaultTarif, 
        IEnumerable<KarcisKomponenType> listKomponen, 
        IEnumerable<LayananReff> listLayanan)
    {
        KarcisId = karcisId;
        KarcisName = karcisName;
        IsAktif = isAktif;
        InstalasiDk = instalasiDk;
        RekapCetak = rekapCetak;
        DefaultTarif = defaultTarif;
        _listKomponen = listKomponen.ToList();
        _listLayanan = listLayanan.ToList();
    }
    public static KarcisType Default => new("-", "-", false, InstalasiDkType.Default, 
        RekapCetakType.Default.ToReff(), TarifType.Default.ToReff(),[],[]);
    public static IKarcisKey Key(string id) => Default with { KarcisId = id };
    #endregion
    
    #region PROPERTIES
    public string KarcisId { get; init; }
    public string KarcisName { get; init; }
    public bool IsAktif { get; init; }
    public InstalasiDkType InstalasiDk { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public TarifReff DefaultTarif { get; init; }
    public IEnumerable<KarcisKomponenType> ListKomponen => _listKomponen;
    public IEnumerable<LayananReff> ListLayanan => _listLayanan;
    public decimal NilaiKarcis => ListKomponen.Sum(x => x.Nilai);
    #endregion
    
    #region BEHAVIOUR
    public KarcisReff ToReff() => new(KarcisId, KarcisName);
    #endregion
}

public interface IKarcisKey
{
    string KarcisId {get;}
}

public record KarcisReff(string KarcisId, string KarcisName);