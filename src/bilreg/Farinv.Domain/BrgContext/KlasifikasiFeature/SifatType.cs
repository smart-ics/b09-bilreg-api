using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record SifatType : ISifatKey
{
    #region CREATION
    public SifatType(string sifatId, string sifatName)
    {
        SifatId = sifatId;
        SifatName = sifatName;
    }

    public static SifatType Create(string sifatId, string sifatName)
    {
        Guard.Against.NullOrWhiteSpace(sifatId, nameof(sifatId));
        Guard.Against.NullOrWhiteSpace(sifatName, nameof(sifatName));
        return new SifatType(sifatId, sifatName);
    }

    public static SifatType Default => new("-", "-");

    public static ISifatKey Key(string id) => Default with { SifatId = id };
    #endregion

    #region PROPERTIES
    public string SifatId { get; init; }
    public string SifatName { get; init; }
    #endregion
}

public interface ISifatKey
{
    string SifatId { get; }
}
