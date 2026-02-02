using Bilreg.Application.AccountingContext.JurnalFeature.UnitGrupAgg;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public class UnitGrupRepo : IUnitGrupRepo
{
    private readonly IUnitGrupDal _unitGrupDal;
    public UnitGrupRepo(IUnitGrupDal unitGrupDal)
    {
        _unitGrupDal = unitGrupDal;
    }
    public MayBe<UnitGrupType> LoadEntity(IUnitGrupKey key)
    {
        var dto = _unitGrupDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }
    public IEnumerable<UnitGrupType> ListData()
    {
        var listDto = _unitGrupDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
