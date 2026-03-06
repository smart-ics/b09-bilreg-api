using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AccountingContext.UnitFeature;

public class JkRepo : IJkRepo
{
    private readonly IJkDal _jkDal;
    public JkRepo(IJkDal jkDal)
    {
        _jkDal = jkDal;
    }
    public MayBe<JkType> LoadEntity(IJkKey key)
    {
        var dto = _jkDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }
    public IEnumerable<JkType> ListData()
    {
        var listDto = _jkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
