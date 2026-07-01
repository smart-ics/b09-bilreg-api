namespace Bilreg.Domain.AdmisiContext.RegFeature;

public record JenisInapType : IJenisInapKey
{

    #region CREATION
    public JenisInapType(string jenisInapId, string jenisInapName)
    {
        JenisInapId = jenisInapId;
        JenisInapName = jenisInapName;
    }

    public static IJenisInapKey Key(string id)
        => Default with { JenisInapId = id };

    public static JenisInapType Default => new("-", "-");

    #endregion

    #region PROPERTIES
    public string JenisInapId { get; init; }
    public string JenisInapName { get; init; }
    #endregion

    #region BEHAVIOR

    #endregion

}


public interface IJenisInapKey { string JenisInapId { get; } }