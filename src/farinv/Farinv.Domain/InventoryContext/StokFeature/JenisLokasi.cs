using Ardalis.GuardClauses;

namespace Farinv.Domain.InventoryContext.StokFeature;

public record JenisLokasiType : IJenisLokasiKey
{
    #region CREATION
    public JenisLokasiType(string jenisLokasiId, string jenisLokasiName)
    {
        Guard.Against.NullOrWhiteSpace(jenisLokasiId);
        Guard.Against.NullOrWhiteSpace(jenisLokasiName);
        JenisLokasiId = jenisLokasiId;
        JenisLokasiName = jenisLokasiName;
    }
    public static JenisLokasiType Default => new("-", "-");
    public static IJenisLokasiKey Key(string id) => Default with { JenisLokasiId = id };
    
    public static JenisLokasiType Gudang = new JenisLokasiType("GDN", "Gudang");
    public static JenisLokasiType Apotek = new JenisLokasiType("APT", "Apotek");
    public static JenisLokasiType CareUnit = new JenisLokasiType("CAR", "Care Unit");
    public static JenisLokasiType Gizi = new JenisLokasiType("GIZ", "Gizi");
    #endregion

    #region PROPERTIES
    public string JenisLokasiId { get; init; }
    public string JenisLokasiName { get; init; }
    #endregion
}

public interface IJenisLokasiKey
{
    string JenisLokasiId { get; }
}
