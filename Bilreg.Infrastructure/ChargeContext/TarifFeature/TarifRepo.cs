using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.TarifAgg;

public class TarifRepo : ITarifRepo
{
    private readonly ITarifDal _tarifDal;

    public TarifRepo(ITarifDal tarifDal)
    {
        _tarifDal = tarifDal;
    }

    public MayBe<TarifType> LoadEntity(ITarifKey key)
    {
        var dto = _tarifDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public IEnumerable<TarifType> ListData()
    {
        var listDto = _tarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }

    public IEnumerable<TarifType> ListData(string filter)
    {
        var listDto = _tarifDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
