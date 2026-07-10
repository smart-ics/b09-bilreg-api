namespace Bilreg.Domain.AdmisiContext.RegFeature;

public record ProsedurMasukInapType : IProsedurMasukInapKey
{
    #region CREATION
    public ProsedurMasukInapType(string prosedurMasukInapId,
        string prosedurMasukInapName, ProsedurMasukInapDkType prosedurMasukInapDk)
    {
        ProsedurMasukInapId = prosedurMasukInapId;
        ProsedurMasukInapName = prosedurMasukInapName;
        ProsedurMasukInapDk = prosedurMasukInapDk;
    }
    public static ProsedurMasukInapType Default 
        => new("-", "-", ProsedurMasukInapDkType.Default);

    public static IProsedurMasukInapKey Key(string id) 
        => Default with { ProsedurMasukInapId = id };

    #endregion

    #region PROPERTY
    public string ProsedurMasukInapId { get; init; }
    public string ProsedurMasukInapName { get; init; }
    public ProsedurMasukInapDkType ProsedurMasukInapDk { get; init; }

    #endregion

    #region BEHAVIOR
    public ProsedurMasukInapReff ToReff()
        => new(ProsedurMasukInapId, ProsedurMasukInapName);
    #endregion
}

public record ProsedurMasukInapDkType(string ProsedurMasukInapDkId, string ProsedurMasukInapDkName)
{
    public static ProsedurMasukInapDkType Default => new("-", "-");
};

public interface IProsedurMasukInapKey
{
    string ProsedurMasukInapId { get; }
}
public record ProsedurMasukInapReff(string ProsedurMasukInapId, string ProsedurMasukInapName);