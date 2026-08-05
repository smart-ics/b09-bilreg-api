using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class KelompokRepo : IKelompokRepo
{
    private readonly IKelompokDal _kelompokDal;

    public KelompokRepo(IKelompokDal kelompokDal)
    {
        _kelompokDal = kelompokDal;
    }

    public void SaveChanges(KelompokType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _kelompokDal.Update(KelompokDto.FromModel(model)),
                onNone: () => _kelompokDal.Insert(KelompokDto.FromModel(model)));
    }

    public MayBe<KelompokType> LoadEntity(IKelompokKey key)
    {
        var dto = _kelompokDal.GetData(key);
        if (dto is null)
            return MayBe<KelompokType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IKelompokKey key)
    {
        _kelompokDal.Delete(key);
    }

    public IEnumerable<KelompokType> ListData()
    {
        var listDto = _kelompokDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}