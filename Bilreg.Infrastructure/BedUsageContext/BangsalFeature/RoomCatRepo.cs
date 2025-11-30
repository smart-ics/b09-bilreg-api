using Bilreg.Application.BedUsageContext.BangsalFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.BangsalFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class RoomCatRepo : IRoomCatRepo
{
    private readonly IRoomCatDal _roomCatDal;
    
    public RoomCatRepo(IRoomCatDal roomCatDal)
    {
        _roomCatDal = roomCatDal;
    }

    public void SaveChanges(RoomCatModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _roomCatDal.Update(RoomCatDto.FromModel(model)),
                onNone: () => _roomCatDal.Insert(RoomCatDto.FromModel(model))
            );
    }

    public MayBe<RoomCatModel> LoadEntity(IRoomCatKey key)
    {
        var dto = _roomCatDal.GetData(key);
        if (dto == null)
            return MayBe<RoomCatModel>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IRoomCatKey key)
    {
        _roomCatDal.Delete(key);
    }

    public IEnumerable<RoomCatModel> ListData()
    {
        var listDto = _roomCatDal.ListData().ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}