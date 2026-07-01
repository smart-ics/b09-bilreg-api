using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class GolonganRepo : IGolonganRepo
{
    private readonly IGolonganDal _golonganDal;

    public GolonganRepo(IGolonganDal golonganDal)
    {
        _golonganDal = golonganDal;
    }

    public void SaveChanges(GolonganType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _golonganDal.Update(GolonganDto.FromModel(model)),
                onNone: () => _golonganDal.Insert(GolonganDto.FromModel(model)));
    }

    public MayBe<GolonganType> LoadEntity(IGolonganKey key)
    {
        var dto = _golonganDal.GetData(key);
        if (dto is null)
            return MayBe<GolonganType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGolonganKey key)
    {
        _golonganDal.Delete(key);
    }

    public IEnumerable<GolonganType> ListData()
    {
        var listDto = _golonganDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}