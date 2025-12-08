using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpRepo : IScheduleOpRepo
{
    private readonly IScheduleOpDal _scheduleOpDal;
    private readonly IScheduleOpPpaDal _scheduleOpPpaDal;
    public ScheduleOpRepo(IScheduleOpDal scheduleOpDal, 
        IScheduleOpPpaDal scheduleOpPpaDal)
    {
        _scheduleOpDal = scheduleOpDal;
        _scheduleOpPpaDal = scheduleOpPpaDal;
    }
    public void SaveChanges(ScheduleOpModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _scheduleOpDal.Update(ScheduleOpDto.FromModel(model)),
                onNone: () => _scheduleOpDal.Insert(ScheduleOpDto.FromModel(model)));
    }

    public MayBe<ScheduleOpModel> LoadEntity(IScheduleOpKey key)
    {   
        var dto = _scheduleOpDal.GetData(key);
        if (dto is null)
            return MayBe<ScheduleOpModel>.None;

        var listPpaDto = _scheduleOpPpaDal.ListData(key)?.ToList() ?? [];
        var listPpa = listPpaDto.Select(x => x.ToModel());
        
        var model = dto.ToModel(listPpa);
        return MayBe.From(model);
    }

    public void DeleteEntity(IScheduleOpKey key)
    {
        _scheduleOpDal.Delete(key);
    }

    public IEnumerable<ScheduleOpView> ListData(DateTime filter)
    {
        var dto = _scheduleOpDal.ListData(filter)?.ToList() ?? [];
        return dto.Select(x => x.ToView());
    }
}