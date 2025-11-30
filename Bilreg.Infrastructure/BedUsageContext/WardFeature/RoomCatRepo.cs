using Bilreg.Application.BedUsageContext.BangsalFeature;
using Bilreg.Domain.BedUsageContext.BangsalFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BangsalFeature;

public class RoomCatRepo : IRoomCatRepo
{
    private readonly IRoomCatDal _roomCatDal;
    public RoomCatRepo(IRoomCatDal roomCatDal)
    {
        _roomCatDal = roomCatDal;
    }
    public void SaveChanges(RoomCatType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _roomCatDal.Update(model),
                onNone: () => _roomCatDal.Insert(model));
    }

    public MayBe<RoomCatType> LoadEntity(IRoomCatKey key)
    {   
        var model = _roomCatDal.GetData(key);
        if (model is null)
            return MayBe<RoomCatType>.None;
        var result = model;
        return MayBe.From(result);
    }

    public void DeleteEntity(IRoomCatKey key)
    {
        _roomCatDal.Delete(key);
    }

    public IEnumerable<RoomCatType> ListData()
    {
        var listModel = _roomCatDal.ListData()?.ToList() ?? [];
        var result = listModel;
        return result;
    }
}