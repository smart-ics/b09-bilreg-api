using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record GolonganType : IGolonganKey
{
    #region CREATION
    public GolonganType(string golonganId, string golonganName)
    {
        GolonganId = golonganId;
        GolonganName = golonganName;
    }

    public static GolonganType Create(string golonganId, string golonganName)
    {
        Guard.Against.NullOrWhiteSpace(golonganId, nameof(golonganId));
        Guard.Against.NullOrWhiteSpace(golonganName, nameof(golonganName));
        return new GolonganType(golonganId, golonganName);
    }

    public static GolonganType Default => new("-", "-");

    public static IGolonganKey Key(string id) => Default with { GolonganId = id };
    #endregion

    #region PROPERTIES
    public string GolonganId { get; init; }
    public string GolonganName { get; init; }
    #endregion
}

public interface IGolonganKey
{
    string GolonganId { get; }
}