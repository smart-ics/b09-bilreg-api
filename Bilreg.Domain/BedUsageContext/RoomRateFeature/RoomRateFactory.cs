using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public class RoomRateFactory
{
    public IRoomRate<RoomRateRegulerTipeType> Create(KamarType kamar,
        IEnumerable<RoomRateRegulerTipeType> listTipe)
    {
        return new RoomRateRegulerType(kamar.KamarId, 
            kamar.ToReff(), listTipe);
    }
    public IRoomRate<RoomRateKelasType> Create(KamarType kamar, 
        IEnumerable<RoomRateKelasType> listTipe)
    {
        return new RoomRateFloatingType(kamar.KamarId, kamar.ToReff(), listTipe);
    }
    public IRoomRate<RoomRateDayType> Create(KamarType kamar,  
        IEnumerable<RoomRateDayType> listTipe)
    {
        return new RoomRateDailyType(kamar.KamarId, kamar.ToReff(), listTipe);
    }
}