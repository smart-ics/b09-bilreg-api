using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KecamatanType : IKecamatanKey
{
    public KecamatanType(string kecamatanId, string kecamatanName, KabupatenType kabupaten)
    {
        Guard.Against.NullOrWhiteSpace(kecamatanId, nameof(kecamatanId));
        Guard.Against.NullOrWhiteSpace(kecamatanName, nameof(kecamatanName));
        Guard.Against.Null(kabupaten, nameof(kabupaten));
        KecamatanId = kecamatanId;
        KecamatanName = kecamatanName;
        Kabupaten = kabupaten;
    }
    
    public string KecamatanId { get; init; }
    public string KecamatanName { get; init; }
    public KabupatenType Kabupaten { get; init; }
    
    public static IKecamatanKey Key(string id) => new KecamatanType(id, "-", KabupatenType.Default);
    public static KecamatanType Default => new("-", "-", KabupatenType.Default);
}

public interface IKecamatanKey
{
    string KecamatanId {get;}
}