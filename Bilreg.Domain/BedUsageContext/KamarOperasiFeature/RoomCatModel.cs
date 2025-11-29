namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record RoomCatModel(string RoomCatId, string RoomCatName) : IRoomCatKey
{
    public static RoomCatModel Default => new RoomCatModel("-", "-");
    public static IRoomCatKey Key(string id) => new RoomCatModel(id, "-");
}

public interface IRoomCatKey
{
    string RoomCatId { get; }
}