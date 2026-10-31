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
    public static JenisInapType Empty => new("", "");
    public static JenisInapType Umum => new(InpatientAgePolicy.JENIS_INAP_UMUM, "Umum");
    public static JenisInapType Obstetri => new(InpatientAgePolicy.JENIS_INAP_OBSTETRI, "Obstetri");
    public static JenisInapType Perinatal => new(InpatientAgePolicy.JENIS_INAP_PERINATAL, "Perinatal");

    public static JenisInapType FromId(string id) => id switch
    {
        InpatientAgePolicy.JENIS_INAP_UMUM => Umum,
        InpatientAgePolicy.JENIS_INAP_OBSTETRI => Obstetri,
        InpatientAgePolicy.JENIS_INAP_PERINATAL => Perinatal,
        _ => Default with { JenisInapId = id }
    };

    #endregion

    #region PROPERTIES
    public string JenisInapId { get; init; }
    public string JenisInapName { get; init; }
    #endregion

    #region BEHAVIOR

    #endregion

}


public interface IJenisInapKey { string JenisInapId { get; } }