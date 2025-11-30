using Ardalis.GuardClauses;
using Bilreg.Domain.BedUsageContext.WardFeature;

public record KamarType : IKamarKey
{
    #region CREATION
    public KamarType(string kamarId, string kamarName, 
        BangsalReff bangsal, KelasReff kelas)
    {
        Guard.Against.NullOrWhiteSpace(kamarId);
        Guard.Against.NullOrWhiteSpace(kamarName);
        Guard.Against.Null(bangsal);
        Guard.Against.Null(kelas);

        KamarId = kamarId;
        KamarName = kamarName;
        Bangsal = bangsal;
        Kelas = kelas;
    }
    public static KamarType Default => new("-", "-", new BangsalReff("-", "-"), new KelasReff("-", "-"));
    public static IKamarKey Key(string id) => Default with { KamarId = id };
    #endregion
    
    #region PROPERTIES
    public string KamarId { get; init; }
    public string KamarName { get; init; }
    public BangsalReff Bangsal { get; init; }
    public KelasReff Kelas { get; init; }
    #endregion
    
    #region BEHAVIOR
    public KamarReff ToReff() => new(KamarId, KamarName);
    #endregion
}

public interface IKamarKey
{
    string KamarId {get;}
}

public record KamarReff(string KamarId, string KamarName);