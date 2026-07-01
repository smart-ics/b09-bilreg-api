using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public record TipeKamarType : ITipeKamarKey
{
    #region CREATION
    public TipeKamarType(string tipeKamarId, string tipeKamarName, 
        bool isDefault, bool isGabung, bool isAktif)
    {
        Guard.Against.NullOrWhiteSpace(tipeKamarId);
        Guard.Against.NullOrWhiteSpace(tipeKamarName);

        TipeKamarId = tipeKamarId;
        TipeKamarName = tipeKamarName;
        IsDefault = isDefault;
        IsGabung = isGabung;
        IsAktif = isAktif;
    }
    public static TipeKamarType Create(string tipeKamarId, string tipeKamarName, 
        bool isDefault, bool isGabung, bool isAktif)
    {
        Guard.Against.NullOrWhiteSpace(tipeKamarId, nameof(tipeKamarId));
        Guard.Against.NullOrWhiteSpace(tipeKamarName, nameof(tipeKamarName));
        return new TipeKamarType(tipeKamarId, tipeKamarName, isDefault, isGabung, isAktif);
    }
    public static TipeKamarType Default => new("-", "-", false, false, false);
    public static ITipeKamarKey Key(string id) => Default with { TipeKamarId = id };
    #endregion
    
    #region PROPERTIES
    public string TipeKamarId { get; init; }
    public string TipeKamarName { get; init; }
    public bool IsDefault { get; init; }
    public bool IsGabung { get; init; }
    public bool IsAktif { get; init; }
    #endregion
    
    #region BEHAVIOUR
    public TipeKamarReff ToReff() => new(TipeKamarId, TipeKamarName, IsAktif);
    #endregion
}

public interface ITipeKamarKey
{
    string TipeKamarId {get;}
}

public record TipeKamarReff(string TipeKamarId, string TipeKamarName, bool IsAktif);