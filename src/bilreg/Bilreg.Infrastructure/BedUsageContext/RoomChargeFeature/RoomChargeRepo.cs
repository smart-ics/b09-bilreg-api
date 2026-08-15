using Bilreg.Application.BedUsageContext.RoomChargeFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public class RoomChargeRepo : IRoomChargeRepo
{
    private readonly IRoomChargeDal _roomChargeDal;
    private readonly IRoomChargeKomponenDal _roomChargeKomponenDal;
    public RoomChargeRepo(IRoomChargeDal roomChargeDal, 
        IRoomChargeKomponenDal roomChargeKomponenDal)
    {
        _roomChargeDal = roomChargeDal;
        _roomChargeKomponenDal = roomChargeKomponenDal;
    }

    public void SaveChanges(RoomChargeModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _roomChargeDal.Update(RoomChargeDto.FromModel(model)),
                onNone: () => _roomChargeDal.Insert(RoomChargeDto.FromModel(model)));


        var listKomponenDto = model.ListKomponen
            .Select(x => RoomChargeKomponenDto.FromModel(x, model.RoomChargeId));
        _roomChargeKomponenDal.Delete(model);
        _roomChargeKomponenDal.Insert(listKomponenDto);
    }
    public MayBe<RoomChargeModel> LoadEntity(IRoomChargeKey key)
    {
        var data = _roomChargeDal.GetData(key);
        if (data is null)
            return MayBe<RoomChargeModel>.None;

        var listKomp = _roomChargeKomponenDal.ListData(key)?.ToList() ?? [];
        var result = data.ToModel(listKomp.Select(x => x.ToModel()));

        return MayBe.From(result);
    }

    public void Delete(IRoomChargeKey key)
    {
        _roomChargeDal.Delete(key);
        _roomChargeKomponenDal.Delete(key);
    }

    public IEnumerable<RoomChargeView> ListData(IRegKey regKey)
    {
        var listDto = _roomChargeDal.ListData(regKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView());
        return result;
    }

    public IEnumerable<RoomChargeView> ListData(IPakaiBed pakaiBedKey)
    {
        var listDto = _roomChargeDal.ListData(pakaiBedKey)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToView());
        return result;
    }

    
}
