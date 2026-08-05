using Ardalis.GuardClauses;

namespace Farinv.Domain.BrgContext.KlasifikasiFeature;

public record GenerikType : IGenerikKey
{
    #region CREATION
    public GenerikType(string generikId, string generikName, string komposisi)
    {
        Guard.Against.NullOrWhiteSpace(generikId, nameof(generikId));
        Guard.Against.NullOrWhiteSpace(generikName, nameof(generikName));
        Guard.Against.NullOrWhiteSpace(komposisi, nameof(komposisi));

        GenerikId = generikId;
        GenerikName = generikName;
        Komposisi = komposisi;
    }

    public static GenerikType Create(string generikId, string generikName, string komposisi)
    {
        return new GenerikType(generikId, generikName, komposisi);
    }

    public static GenerikType Default => new("-", "-", "-");

    public static IGenerikKey Key(string id) => Default with { GenerikId = id };
    #endregion

    #region PROPERTIES
    public string GenerikId { get; init; }
    public string GenerikName { get; init; }
    public string Komposisi { get; init; }
    #endregion

    #region BEHAVIOR
    public GenerikReff ToReff() => new(GenerikId, GenerikName);
    #endregion
}

public interface IGenerikKey
{
    string GenerikId { get; }
}

public record GenerikReff(string GenerikId, string GenerikName);