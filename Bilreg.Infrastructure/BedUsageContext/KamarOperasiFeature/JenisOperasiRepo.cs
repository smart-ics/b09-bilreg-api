using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class JenisOperasiRepo : IJenisOperasiRepo
{
    private readonly IJenisOperasiDal _jenisOperasiDal;

    public JenisOperasiRepo(IJenisOperasiDal jenisOperasiDal)
    {
        _jenisOperasiDal = jenisOperasiDal;
    }

    public void SaveChanges(JenisOperasiType model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _jenisOperasiDal.Update(JenisOperasiDto.FromModel(x)),
                onNone: () => _jenisOperasiDal.Insert(JenisOperasiDto.FromModel(model))
            );
    }
    public MayBe<JenisOperasiType> LoadEntity(IJenisOperasiKey key)
    {
        var result = _jenisOperasiDal.GetData(key);
        return MayBe.From(result.ToModel());
    }

    public void DeleteEntity(IJenisOperasiKey key)
    {
        _jenisOperasiDal.Delete(key);
    }

    public IEnumerable<JenisOperasiType> ListData()
    {
        var listDto = _jenisOperasiDal.ListData();
        var result = listDto.Select(x => x.ToModel());
        return result;
    }

}