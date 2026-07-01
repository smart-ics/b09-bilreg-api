using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public class MapJaminanJkRepo : IMapJaminanJkRepo
{
    private readonly IMapJaminanJkDal _mapJaminanJkDal;
    public MapJaminanJkRepo(IMapJaminanJkDal mapJaminanJkDal)
    {
        _mapJaminanJkDal = mapJaminanJkDal;
    }
    public MayBe<MapJaminanJkType> LoadEntity(IJaminanKey key)
    {
        var dto = _mapJaminanJkDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }
    public IEnumerable<MapJaminanJkType> ListData()
    {
        var listDto = _mapJaminanJkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
