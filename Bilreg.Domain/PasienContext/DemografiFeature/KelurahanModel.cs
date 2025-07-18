using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext.DemografiFeature;

public record KelurahanType : IKelurahanKey
{
    public KelurahanType(string kelurahanId, string kelurahanName, KecamatanType kecamatan)
    {
        Guard.Against.NullOrWhiteSpace(kelurahanId, nameof(kelurahanId));
        Guard.Against.NullOrWhiteSpace(kelurahanName, nameof(kelurahanName));
        Guard.Against.Null(kecamatan, nameof(kecamatan));

        KelurahanId = kelurahanId;
        KelurahanName = kelurahanName;
        Kecamatan = kecamatan;
    }
    
    public string KelurahanId { get; init; }
    public string KelurahanName { get; init; }
    public KecamatanType Kecamatan { get; init; }
    
    public static IKelurahanKey Key(string id) => new KelurahanType(id, "-", KecamatanType.Default);
    public static KelurahanType Default => new("-", "-", KecamatanType.Default);
}

public interface IKelurahanKey
{
    string KelurahanId {get;}
}