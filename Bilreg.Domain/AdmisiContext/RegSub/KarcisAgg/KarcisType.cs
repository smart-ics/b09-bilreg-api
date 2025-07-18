using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.BillContext.RekapCetakSub;
using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public record KarcisType : IKarcisKey
{
    public KarcisType(string karcisId, string karcisName, bool isAktif, 
        InstalasiDkType instalasiDk, RekapCetakReff rekapCetak, TarifReff defaultTarif, 
        IList<KarcisKomponenModel> listKomponen, 
        IList<LayananReff> listLayanan)
    {
        Guard.Against.NullOrWhiteSpace(karcisId, nameof(karcisId));
        Guard.Against.NullOrWhiteSpace(karcisName, nameof(karcisName));
        Guard.Against.Null(instalasiDk, nameof(instalasiDk));
        Guard.Against.Null(rekapCetak, nameof(rekapCetak));
        Guard.Against.Null(defaultTarif, nameof(defaultTarif));
        Guard.Against.Null(listKomponen, nameof(listKomponen));
        Guard.Against.Null(listLayanan, nameof(listLayanan));

        KarcisId = karcisId;
        KarcisName = karcisName;
        IsAktif = isAktif;
        InstalasiDk = instalasiDk;
        RekapCetak = rekapCetak;
        DefaultTarif = defaultTarif;
        ListKomponen = listKomponen.ToList();
        ListLayanan = listLayanan.ToList();
    }
    
    public string KarcisId { get; init; }
    public string KarcisName { get; init; }
    public bool IsAktif { get; init; }
    public InstalasiDkType InstalasiDk { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public TarifReff DefaultTarif { get; init; }
    public IReadOnlyList<KarcisKomponenModel> ListKomponen { get; init; }
    public IReadOnlyList<LayananReff> ListLayanan { get; init; }

    
    public decimal GetNilai() => ListKomponen.Sum(x => x.Nilai);
    public KarcisReff ToReff() => new(KarcisId, KarcisName);
    
    public static KarcisType Default => new("-", "-", false, InstalasiDkType.Default, 
        RekapCetakType.Default.ToReff(), TarifType.Default.ToReff(), 
        new List<KarcisKomponenModel>(),
        new List<LayananReff>());
    public static IKarcisKey Key(string id) => Default with { KarcisId = id };
}

public interface IKarcisKey
{
    string KarcisId {get;}
}

public record KarcisReff(string KarcisId, string KarcisName);