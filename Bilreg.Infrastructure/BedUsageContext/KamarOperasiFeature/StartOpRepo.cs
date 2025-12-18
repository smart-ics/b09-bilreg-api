using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class StartOpRepo : IStartOpRepo
{
    private readonly IStartOpDal _startOpDal;

    public StartOpRepo(IStartOpDal startOpDal)
    {
        _startOpDal = startOpDal;
    }

    public void SaveChanges(StartOpModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: x => _startOpDal.Update(StartOpDto.FromModel(x)),
                onNone: () => _startOpDal.Insert(StartOpDto.FromModel(model))
            );
    }

    public MayBe<StartOpModel> LoadEntity(IStartOpKey key)
    {
        var result = _startOpDal.GetData(key);
        return MayBe.From(result.ToModel());
    }

    public void DeleteEntity(IStartOpKey key)
    {
        _startOpDal.Delete(key);
    }
}
