using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KecamatanType : IKecamatanKey
{
    public KecamatanType(string kecamatanId, string kecamatanName, 
        KabupatenReff kabupaten, PropinsiType propinsi)
    {
        KecamatanId = kecamatanId;
        KecamatanName = kecamatanName;
        Kabupaten = kabupaten;
        Propinsi = propinsi;
    }
    
    public string KecamatanId { get; init; }
    public string KecamatanName { get; init; }
    public KabupatenReff Kabupaten { get; init; }
    public PropinsiType Propinsi { get; init; }
    
    public KecamatanReff ToReff() => new(KecamatanId, KecamatanName);
    
    public static KecamatanType Default => new("-", "-", KabupatenType.Default.ToReff(), PropinsiType.Default);
    public static IKecamatanKey Key(string id) => Default with { KecamatanId = id };
}

public interface IKecamatanKey
{
    string KecamatanId {get;}
}

public record KecamatanReff(string KecamatanId, string KecamatanName);