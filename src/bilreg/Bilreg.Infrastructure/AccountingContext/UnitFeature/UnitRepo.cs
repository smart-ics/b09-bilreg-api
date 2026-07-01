using Bilreg.Application.AccountingContext.JurnalFeature.UnitAgg;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public class UnitRepo : IUnitRepo
{
    private readonly IUnitDal _unitDal;
   public UnitRepo(IUnitDal unitDal)
    {
        _unitDal = unitDal;
    }

    public MayBe<UnitType> LoadEntity(IUnitKey key)
    {
        var dto = _unitDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }
    public IEnumerable<UnitType> ListData()
    {
        var listDto = _unitDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
