namespace Bilreg.Domain.BedUsageContext.WardFeature;

public record KelasType : IKelasKey
{
    #region CREATION
    public KelasType(string kelasId, string kelasName, bool isAktif, KelasDkType kelasDk)
    {
        KelasId = kelasId;
        KelasName = kelasName;
        IsAktif = isAktif;
        KelasDk = kelasDk;
    }
    public static KelasType Default => new KelasType("-", "-", false, KelasDkType.Default);
    public static IKelasKey Key(string id) => Default with { KelasId = id };
    
    #endregion
    
    #region PROPERTIES
    public string KelasId { get; init; }
    public string KelasName { get; init; }
    public bool IsAktif { get; init; }
    public KelasDkType KelasDk { get; init; }
    #endregion
    
    #region BEHAVIOUR
    public KelasReff ToReff()
        => new KelasReff(KelasId, KelasName);
    #endregion
}

public interface IKelasKey
{
    string KelasId { get; }
}

public record KelasReff(string KelasId, string KelasName) : IKelasKey;