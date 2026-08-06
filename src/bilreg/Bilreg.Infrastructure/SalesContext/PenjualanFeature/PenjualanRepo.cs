using Bilreg.Application.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.Shared;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public class PenjualanRepo : IPenjualanRepo
{
    private readonly IPenjualanDal _penjualanDal;
    private readonly IPenjualanItemDal _penjualanItemDal;

    public PenjualanRepo(IPenjualanDal penjualanDal, IPenjualanItemDal penjualanItemDal)
    {
        _penjualanDal = penjualanDal;
        _penjualanItemDal = penjualanItemDal;
    }

    public void SaveChanges(PenjualanModel model)
    {
        var dto = PenjualanDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _penjualanDal.Update(dto),
                onNone: () => _penjualanDal.Insert(dto));

        var listItemDto = PenjualanItemDto.FlattenFromModel(model);
        _penjualanItemDal.Delete(model);
        _penjualanItemDal.Insert(listItemDto);
    }

    public MayBe<PenjualanModel> LoadEntity(IPenjualanKey key)
    {
        var dto = _penjualanDal.GetData(key);
        if (dto is null)
            return MayBe<PenjualanModel>.None;

        var listItem = _penjualanItemDal.ListData(key)?.ToList() ?? [];
        var model = dto.ToModel(listItem);
        return MayBe.From(model);
    }

    public void DeleteEntity(IPenjualanKey key)
    {
        _penjualanItemDal.Delete(key);
        _penjualanDal.Delete(key);
    }

    public IEnumerable<PenjualanModel> ListData(IRegKey filter)
    {
        var listDto = _penjualanDal.ListData(filter)?.ToList() ?? [];
        var result = new List<PenjualanModel>();

        foreach (var dto in listDto)
        {
            var listItem = _penjualanItemDal.ListData(PenjualanModel.Key(dto.PenjualanId))?.ToList() ?? [];
            result.Add(dto.ToModel(listItem));
        }

        return result;
    }
}
