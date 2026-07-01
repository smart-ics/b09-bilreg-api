using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class SifatRepo : ISifatRepo
{
    private readonly ISifatDal _sifatDal;

    public SifatRepo(ISifatDal sifatDal)
    {
        _sifatDal = sifatDal;
    }

    public void SaveChanges(SifatType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _sifatDal.Update(SifatDto.FromModel(model)),
                onNone: () => _sifatDal.Insert(SifatDto.FromModel(model)));
    }

    public MayBe<SifatType> LoadEntity(ISifatKey key)
    {
        var dto = _sifatDal.GetData(key);
        if (dto is null)
            return MayBe<SifatType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ISifatKey key)
    {
        _sifatDal.Delete(key);
    }

    public IEnumerable<SifatType> ListData()
    {
        var listDto = _sifatDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}