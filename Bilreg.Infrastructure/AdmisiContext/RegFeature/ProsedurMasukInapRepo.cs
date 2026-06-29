using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class ProsedurMasukInapRepo : IProsedureMasukInapRepo
{
    private readonly IProsedurMasukInapDal _prosedurMasukInapDal;

    public ProsedurMasukInapRepo(IProsedurMasukInapDal prosedurMasukInapDal)
    {
        _prosedurMasukInapDal = prosedurMasukInapDal;
    }

    public MayBe<ProsedurMasukInapType> LoadEntity(IProsedurMasukInapKey key)
    {
        var data = _prosedurMasukInapDal.GetData(key);
        if (data is null)
            return MayBe<ProsedurMasukInapType>.None;
        var model = data.ToModel();
        return MayBe.From(model);
    }

    public IEnumerable<ProsedurMasukInapType> ListData()
    {
        var listDto = _prosedurMasukInapDal.ListData() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
