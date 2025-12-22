using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

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
                onSome: _ => _startOpDal.Update(StartOpDto.FromModel(model)),
                onNone: () => _startOpDal.Insert(StartOpDto.FromModel(model))
            );
    }

    public MayBe<StartOpModel> LoadEntity(IStartOpKey key)
    {
        var dto = _startOpDal.GetData(key);
        if (dto is null)
            return MayBe<StartOpModel>.None;

        var result = dto.ToModel();
        return MayBe.From(result);
    }

    public void DeleteEntity(IStartOpKey key)
    {
        _startOpDal.Delete(key);
    }

    public IEnumerable<StartOpView> ListData(DateTime filter)
    {
        var listDto = _startOpDal.ListData(filter)?.ToList() ?? [];
        var result = listDto.Select(x =>
            new StartOpView(x.StartOpId, x.ScheduleOpId, x.OrderOpId, x.StartOpTime, x.RegId, x.PasienId, x.PasienName));

        return result;
    }
}
