using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record KelompokType : IKelompokKey
{
    #region CREATION
    public KelompokType(string kelompokId, string kelompokName)
    {
        KelompokId = kelompokId;
        KelompokName = kelompokName;
    }

    public static KelompokType Create(string kelompokId, string kelompokName)
    {
        Guard.Against.NullOrWhiteSpace(kelompokId, nameof(kelompokId));
        Guard.Against.NullOrWhiteSpace(kelompokName, nameof(kelompokName));
        return new KelompokType(kelompokId, kelompokName);
    }

    public static KelompokType Default => new("-", "-");

    public static IKelompokKey Key(string id) => Default with { KelompokId = id };
    #endregion

    #region PROPERTIES
    public string KelompokId { get; init; }
    public string KelompokName { get; init; }
    #endregion
}

public interface IKelompokKey
{
    string KelompokId { get; }
}