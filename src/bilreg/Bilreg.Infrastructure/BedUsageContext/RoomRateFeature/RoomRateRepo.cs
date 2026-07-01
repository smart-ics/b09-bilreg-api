using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;

public class RoomRateRepo : IRoomRateRepo
{
    private readonly IRoomRateDal _roomRateDal;
    private readonly IKamarRepo _kamarRepo;

    public RoomRateRepo(IRoomRateDal roomRateDal, IKamarRepo kamarRepo)
    {
        _roomRateDal = roomRateDal;
        _kamarRepo = kamarRepo;
    }

    public void SaveChanges(IRoomRate<IRoomRateDetail> model)
    {
        _roomRateDal.Delete(model);
        IEnumerable<RoomRateDto>? listDto = null;
        switch (model)
        {
            case RoomRateRegulerType or RoomRateDailyType:
            {
                var kelas = _kamarRepo.LoadEntity(model)
                    .GetValueOrThrow("Invalid Kode Kamar")
                    .Kelas;
                listDto = model switch
                {
                    RoomRateRegulerType regulerType => RoomRateDto.FromModel(regulerType, kelas),
                    RoomRateDailyType dailyType => RoomRateDto.FromModel(dailyType, kelas),
                    _ => listDto
                };
                break;
            }
            case RoomRateFloatingType floatingType:
                listDto = RoomRateDto.FromModel(floatingType);
                break;
        }
        if (listDto is not null)
            _roomRateDal.Insert(listDto);
    }

    public MayBe<IRoomRate<IRoomRateDetail>> LoadEntity(IKamarKey key)
    {
        var listDto = _roomRateDal.ListData(key);
        if (listDto is null)
            return MayBe<IRoomRate<IRoomRateDetail>>.None;

        var model = RoomRateDto.ToModel(listDto);
        var result =  MayBe<IRoomRate<IRoomRateDetail>>.Some(model);
        return result;
    }

    public void DeleteEntity(IKamarKey key)
    {
        _roomRateDal.Delete(key);
    }
}
