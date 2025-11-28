using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.JaminanAgg;

public class JaminanRepo : IJaminanRepo
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanRepo(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public MayBe<JaminanType> LoadEntity(IJaminanKey key)
    {
        var dto = _jaminanDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public IEnumerable<JaminanType> ListData()
    {
        var listDto = _jaminanDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
