using Farinv.Application.BrgContext.StandardFeature;
using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public class KelasTerapiRepo : IKelasTerapiRepo
{
    private readonly IKelasTerapiDal _kelasTerapiDal;

    public KelasTerapiRepo(IKelasTerapiDal kelasTerapiDal)
    {
        _kelasTerapiDal = kelasTerapiDal;
    }

    public void SaveChanges(KelasTerapiType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _kelasTerapiDal.Update(KelasTerapiDto.FromModel(model)),
                onNone: () => _kelasTerapiDal.Insert(KelasTerapiDto.FromModel(model)));
    }

    public MayBe<KelasTerapiType> LoadEntity(IKelasTerapiKey key)
    {
        var dto = _kelasTerapiDal.GetData(key);
        if (dto is null)
            return MayBe<KelasTerapiType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IKelasTerapiKey key)
    {
        _kelasTerapiDal.Delete(key);
    }

    public IEnumerable<KelasTerapiType> ListData()
    {
        var listDto = _kelasTerapiDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
