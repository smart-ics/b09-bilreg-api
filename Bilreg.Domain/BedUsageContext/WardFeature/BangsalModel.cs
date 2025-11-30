using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

public record BangsalType : IBangsalKey
{
    #region CREATION
    public BangsalType(string bangsalId, string bangsalName, 
        RoomCatType roomCat, LayananReff layanan)
    {
        Guard.Against.NullOrWhiteSpace(bangsalId);
        Guard.Against.NullOrWhiteSpace(bangsalName);
        Guard.Against.Null(roomCat);
        Guard.Against.Null(layanan);

        BangsalId = bangsalId;
        BangsalName = bangsalName;
        RoomCat = roomCat;
        Layanan = layanan;
    }
    public static BangsalType Default => new("-", "-", RoomCatType.Default, new LayananReff("-", "-"));
    public static IBangsalKey Key(string id) => Default with { BangsalId = id };
    #endregion
    
    #region PROPERTIES
    public string BangsalId { get; init; }
    public string BangsalName { get; init; }
    public RoomCatType RoomCat { get; init; }
    public LayananReff Layanan { get; init; }
    #endregion
    
    #region BEHAVIOR
    public BangsalReff ToReff() => new(BangsalId, BangsalName);
    #endregion
}

public interface IBangsalKey
{
    string BangsalId {get;}
}

public record BangsalReff(string BangsalId, string BangsalName);