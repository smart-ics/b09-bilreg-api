using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.WardFeature;

public record BedType : IBedKey
{
    #region CREATION
    public BedType(string bedId, string bedName, 
        KamarReff kamar, BangsalReff bangsal, bool isAktif)
    {
        Guard.Against.NullOrWhiteSpace(bedId);
        Guard.Against.NullOrWhiteSpace(bedName);
        Guard.Against.Null(kamar);
        Guard.Against.Null(bangsal);

        BedId = bedId;
        BedName = bedName;
        Kamar = kamar;
        Bangsal = bangsal;
        IsAktif = isAktif;
    }
    public static BedType Default => new("-", "-", new KamarReff("-", "-"), new BangsalReff("-", "-"), false);
    public static IBedKey Key(string id) => Default with { BedId = id };
    #endregion
    
    #region PROPERTIES
    public string BedId { get; init; }
    public string BedName { get; init; }
    public KamarReff Kamar { get; init; }
    public BangsalReff Bangsal { get; init; }
    public bool IsAktif { get; init; }
    #endregion
    
    #region BEHAVIOR
    public BedReff ToReff() => new(BedId, BedName, IsAktif);
    #endregion
}

public interface IBedKey
{
    string BedId {get;}
}

public record BedReff(string BedId, string BedName, bool IsAktif);