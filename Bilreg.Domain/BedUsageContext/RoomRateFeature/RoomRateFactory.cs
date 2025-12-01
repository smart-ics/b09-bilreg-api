using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public class RoomRateFactory
{
    public IRoomRate<IRoomRateDetail> Create(KamarType kamar, IEnumerable<IRoomRateDetail> listDetil)
    {
        return listDetil switch
        {
            IEnumerable<RoomRateKelasType> kelas => CreateKelas(kamar, kelas),
            IEnumerable<RoomRateDayType> day     => CreateDaily(kamar, day),
            IEnumerable<RoomRateRegulerTipeType> reg => CreateReguler(kamar, reg),
            _ => throw new Exception("Unsupported detail type")
        };
    }    
    
    private IRoomRate<RoomRateRegulerTipeType> CreateReguler(KamarType kamar, IEnumerable<RoomRateRegulerTipeType> listTipe)
    {
        return new RoomRateRegulerType(kamar.KamarId, kamar.ToReff(), listTipe); 
        
    }

    private IRoomRate<RoomRateKelasType> CreateKelas(KamarType kamar, IEnumerable<RoomRateKelasType> listTipe)
    {
        return new RoomRateFloatingType(kamar.KamarId, kamar.ToReff(), listTipe); 
        
    }

    private IRoomRate<RoomRateDayType> CreateDaily(KamarType kamar, IEnumerable<RoomRateDayType> listTipe)
    {
        return new RoomRateDailyType(kamar.KamarId, kamar.ToReff(), listTipe); 
    } 

    
}
