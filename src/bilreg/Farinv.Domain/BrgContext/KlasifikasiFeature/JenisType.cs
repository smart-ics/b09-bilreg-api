using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record JenisType : IJenisKey
{
    #region CREATION
    public JenisType(string jenisId, string jenisName)
    {
        JenisId = jenisId;
        JenisName = jenisName;
    }

    public static JenisType Create(string jenisId, string jenisName)
    {
        Guard.Against.NullOrWhiteSpace(jenisId, nameof(jenisId));
        Guard.Against.NullOrWhiteSpace(jenisName, nameof(jenisName));
        return new JenisType(jenisId, jenisName);
    }

    public static JenisType Default => new("-", "-");

    public static IJenisKey Key(string id) => Default with { JenisId = id };
    #endregion

    #region PROPERTIES
    public string JenisId { get; init; }
    public string JenisName { get; init; }
    #endregion
}

public interface IJenisKey
{
    string JenisId { get; }
}