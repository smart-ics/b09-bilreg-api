namespace Bilreg.Domain.AdmisiContext.RegFeature;

public record ProsedurMasukInapType : IProsedurMasukInapKey
{


    #region CREATION
    public ProsedurMasukInapType(string prosedurMasukInapId,
        string prosedurMasukInapName,
        string prosedurMasukInapDkId,
        string prosedurMasukInapDkName)
    {
        ProsedurMasukInapId = prosedurMasukInapId;
        ProsedurMasukInapName = prosedurMasukInapName;
        ProsedurMasukInapDkId = prosedurMasukInapDkId;
        ProsedurMasukInapDkName = prosedurMasukInapDkName;
    }
    public static ProsedurMasukInapType Default 
        => new("-", "-", "-", "-");

    public static IProsedurMasukInapKey Key(string id) 
        => Default with { ProsedurMasukInapId = id };

    #endregion

    #region PROPERTY
    public string ProsedurMasukInapId { get; init; }
    public string ProsedurMasukInapName { get; init; }
    public string ProsedurMasukInapDkId { get; init; }
    public string ProsedurMasukInapDkName { get; init; }

    #endregion

    #region BEHAVIOR
    public ProsedurMasukInapReff ToReff()
        => new(ProsedurMasukInapId, ProsedurMasukInapName);
    #endregion
}

public interface IProsedurMasukInapKey
{
    string ProsedurMasukInapId { get; }
}
public record ProsedurMasukInapReff(string ProsedurMasukInapId, string ProsedurMasukInapName);