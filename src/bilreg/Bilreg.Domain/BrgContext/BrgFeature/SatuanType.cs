using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;

namespace Bilreg.Domain.BrgContext.BrgFeature;

public record SatuanType : ISatuanKey
{
    #region CREATION
    public SatuanType(string satuanId, string satuanName)
    {
        SatuanId = satuanId;
        SatuanName = satuanName;
    }

    public static SatuanType Create(string satuanId, string satuanName)
    {
        Guard.Against.NullOrWhiteSpace(satuanId);
        Guard.Against.NullOrWhiteSpace(satuanName);
        return new SatuanType(satuanId, satuanName);
    }

    public static SatuanType Default => new("-", "-");

    public static ISatuanKey Key(string id) => Default with { SatuanId = id };

    public SatuanReff ToReff() => new(SatuanId, SatuanName);
    #endregion

    #region PROPERTIES
    public string SatuanId { get; init; }
    public string SatuanName { get; init; }
    #endregion
}

public interface ISatuanKey
{
    string SatuanId { get; }
}

public record SatuanReff(string SatuanId, string SatuanName) : ISatuanKey
{
    public static SatuanReff Default => new("-", "-");
}
