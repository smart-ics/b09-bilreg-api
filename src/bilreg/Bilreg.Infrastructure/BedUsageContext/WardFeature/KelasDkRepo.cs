using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public class KelasDkRepo : IKelasDkRepo
{
    private readonly IKelasDkDal _kelasDkDal;

    public KelasDkRepo(IKelasDkDal kelasDkDal)
    {
        _kelasDkDal = kelasDkDal;
    }

    public MayBe<KelasDkType> LoadEntity(IKelasDkKey key)
    {
        var result = _kelasDkDal.GetData(key);
        var model = result?.ToModel();
        return MayBe.From(model!);
    }

    public IEnumerable<KelasDkType> ListData()
    {
        var result = _kelasDkDal.ListData();
        var model = result.Select(x => x.ToModel());
        return model;
    }
}