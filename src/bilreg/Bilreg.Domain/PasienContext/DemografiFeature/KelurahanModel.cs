using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KelurahanType : IKelurahanKey
{
    public KelurahanType(string kelurahanId, string kelurahanName, 
        KecamatanReff kecamatan, KabupatenReff kabupaten, PropinsiType propinsi)
    {
        KelurahanId = kelurahanId;
        KelurahanName = kelurahanName;
        Kecamatan = kecamatan;
        Kabupaten = kabupaten;
        Propinsi = propinsi;
    }
    
    public string KelurahanId { get; init; }
    public string KelurahanName { get; init; }
    public KecamatanReff Kecamatan { get; init; }
    public KabupatenReff Kabupaten { get; init; }
    public PropinsiType Propinsi { get; init; }
    
    public KelurahanReff ToReff() => new(KelurahanId, KelurahanName);
    
    public static KelurahanType Default => new("-", "-", KecamatanType.Default.ToReff(),
        KabupatenType.Default.ToReff(), PropinsiType.Default);
    public static IKelurahanKey Key(string id) => Default with { KelurahanId = id };
}

public interface IKelurahanKey
{
    string KelurahanId {get;}
}

public record KelurahanReff(string KelurahanId, string KelurahanName)
{
    public static KelurahanReff Default => new("-", "-");
};