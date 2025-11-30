using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.BangsalFeature;

public record RoomCatType : IRoomCatKey
{
    #region CREATION
    public RoomCatType(string roomCatId, string roomCatName)
    {
        RoomCatId = roomCatId;
        RoomCatName = roomCatName;
    }
    public static RoomCatType Create(string roomCatId, string roomCatName)
    {
        Guard.Against.NullOrWhiteSpace(roomCatId);
        Guard.Against.NullOrWhiteSpace(roomCatName);
        return new RoomCatType(roomCatId, roomCatName);
    }
    public static RoomCatType Default => new("-", "-");
    public static IRoomCatKey Key(string id) => Default with { RoomCatId = id };
    #endregion
    
    #region PROPERTIES
    public string RoomCatId { get; init; }
    public string RoomCatName { get; init; }
    #endregion
}

public interface IRoomCatKey
{
    string RoomCatId {get;}
}