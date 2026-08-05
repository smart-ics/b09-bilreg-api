using Ardalis.GuardClauses;

namespace Farinv.Domain.InventoryContext.StokFeature;

public record LayananType : ILayananKey
{
    #region CREATION
    public LayananType(string layananId, string layananName, JenisLokasiType jenisLayanan)
    {

        LayananId = layananId;
        LayananName = layananName;
        JenisLayanan = jenisLayanan;
    }

    public static LayananType Create(string layananId, string layananName, JenisLokasiType jenisLayanan)
    {
        Guard.Against.NullOrWhiteSpace(layananId);
        Guard.Against.NullOrWhiteSpace(layananName);
        Guard.Against.Null(jenisLayanan);
        return new LayananType(layananId, layananName, jenisLayanan);
    }

    public static LayananType Default => new("-", "-", JenisLokasiType.Default);
    public static ILayananKey Key(string id) => Default with { LayananId = id };
    #endregion

    #region PROPERTIES
    public string LayananId { get; init; }
    public string LayananName { get; init; }
    public JenisLokasiType JenisLayanan { get; init; }
    #endregion

    #region BEHAVIOR
    public LayananReff ToReff() => new(LayananId, LayananName);
    #endregion
}

public interface ILayananKey
{
    string LayananId { get; }
}

public record LayananReff(string LayananId, string LayananName);