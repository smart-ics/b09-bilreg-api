using Farinv.Application.BrgContext.StandardFeature;
using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public class GolTerapiRepo : IGolTerapiRepo
{
    private readonly IGolTerapiDal _golTerapiDal;

    public GolTerapiRepo(IGolTerapiDal golTerapiDal)
    {
        _golTerapiDal = golTerapiDal;
    }

    public void SaveChanges(GolTerapiType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _golTerapiDal.Update(GolTerapiDto.FromModel(model)),
                onNone: () => _golTerapiDal.Insert(GolTerapiDto.FromModel(model)));
    }

    public MayBe<GolTerapiType> LoadEntity(IGolTerapiKey key)
    {
        var dto = _golTerapiDal.GetData(key);
        if (dto is null)
            return MayBe<GolTerapiType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGolTerapiKey key)
    {
        _golTerapiDal.Delete(key);
    }

    public IEnumerable<GolTerapiType> ListData()
    {
        var listDto = _golTerapiDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
