using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record RoomCatDto(string RoomCatId, string RoomCatName)
{
    public static RoomCatDto FromModel(RoomCatModel model)
        => new (model.RoomCatId, model.RoomCatName);
    
    public RoomCatModel ToModel()
        => new RoomCatModel(RoomCatId, RoomCatName);
}