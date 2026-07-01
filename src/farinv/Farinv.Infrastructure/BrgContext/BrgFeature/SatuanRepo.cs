using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

public class SatuanRepo : ISatuanRepo
{
    private readonly ISatuanDal _satuanDal;

    public SatuanRepo(ISatuanDal satuanDal)
    {
        _satuanDal = satuanDal;
    }

    public void SaveChanges(SatuanType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _satuanDal.Update(SatuanDto.FromModel(model)),
                onNone: () => _satuanDal.Insert(SatuanDto.FromModel(model)));
    }

    public MayBe<SatuanType> LoadEntity(ISatuanKey key)
    {
        var dto = _satuanDal.GetData(key);
        if (dto is null)
            return MayBe<SatuanType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ISatuanKey key)
    {
        _satuanDal.Delete(key);
    }

    public IEnumerable<SatuanType> ListData()
    {
        var listDto = _satuanDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}