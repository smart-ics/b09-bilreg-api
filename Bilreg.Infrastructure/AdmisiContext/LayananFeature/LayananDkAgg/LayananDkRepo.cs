using Bilreg.Application.AdmisiContext.LayananFeature.TipeLayananDkAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananDkAgg;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature.LayananDkAgg;

public class LayananDkRepo : ILayananDkRepo
{
    private readonly ILayananDkDal _lynDkDal;

    public LayananDkRepo(ILayananDkDal lynDkDal)
    {
        _lynDkDal = lynDkDal;
    }

    public MayBe<LayananDkType> LoadEntity(ILayananDkKey key)
    {
        var dto = _lynDkDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public IEnumerable<LayananDkType> ListData(IInstalasiDkKey filter)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<LayananDkType> ListData()
    {
        var listDto = _lynDkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
