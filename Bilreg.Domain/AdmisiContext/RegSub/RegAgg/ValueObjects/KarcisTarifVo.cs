using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using CommunityToolkit.Diagnostics;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record KarcisTarifVo
{
    public string KarcisId { get; }
    public string KarcisName { get; }
    public string TarifId { get; }
    public string TarifName { get; }
    public decimal KarcisNilai => ListKomponen.Sum(x => x.Nilai);
    public IEnumerable<KomponenNilaiVo> ListKomponen { get; }
    public KarcisTarifVo(KarcisModel karcis)
    {
        Guard.IsNotNull(karcis);
        Guard.IsNotNullOrEmpty(karcis.KarcisId);
        Guard.IsNotNullOrEmpty(karcis.KarcisName);
        
        KarcisId = karcis.KarcisId;
        KarcisName = karcis.KarcisName;
        TarifId = karcis.TarifId;
        TarifName = karcis.TarifName;
        
        var listKarifTarif = karcis.ListKomponen
            .Select(x => new KomponenNilaiVo(x.KomponenId, x.KomponenName, x.Nilai));
        ListKomponen = listKarifTarif;
    }

    public KarcisTarifVo(string karcisId, string karcisName, 
        string tarifId, string tarifName,
        IEnumerable<KomponenNilaiVo> listKomponen)
    {
        KarcisId = karcisId;
        KarcisName = karcisName;
        TarifId = tarifId;
        TarifName = tarifName;
        ListKomponen = listKomponen;
    }
}

public record KomponenNilaiVo(
    string KomponenId, 
    string KomponenName, 
    decimal Nilai)
{
}