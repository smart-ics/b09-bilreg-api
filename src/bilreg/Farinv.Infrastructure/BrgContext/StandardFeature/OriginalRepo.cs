using Farinv.Application.BrgContext.StandardFeature;
using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public class OriginalRepo : IOriginalRepo
{
    private readonly IOriginalDal _originalDal;

    public OriginalRepo(IOriginalDal originalDal)
    {
        _originalDal = originalDal;
    }

    public void SaveChanges(OriginalType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _originalDal.Update(OriginalDto.FromModel(model)),
                onNone: () => _originalDal.Insert(OriginalDto.FromModel(model)));
    }

    public MayBe<OriginalType> LoadEntity(IOriginalKey key)
    {
        var dto = _originalDal.GetData(key);
        if (dto is null)
            return MayBe<OriginalType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IOriginalKey key)
    {
        _originalDal.Delete(key);
    }

    public IEnumerable<OriginalType> ListData()
    {
        var listDto = _originalDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
