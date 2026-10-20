using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpRepo : IScheduleOpRepo
{
    private readonly IScheduleOpDal _scheduleOpDal;
    private readonly IScheduleOpPpaDal _scheduleOpPpaDal;
    private readonly IScheduleOpHistDal _scheduleOpHistDal;

    public ScheduleOpRepo(
        IScheduleOpDal scheduleOpDal, 
        IScheduleOpPpaDal scheduleOpPpaDal,
        IScheduleOpHistDal scheduleOpHistDal)
    {
        _scheduleOpDal = scheduleOpDal;
        _scheduleOpPpaDal = scheduleOpPpaDal;
        _scheduleOpHistDal = scheduleOpHistDal;
    }

    public void SaveChanges(ScheduleOpModel model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _scheduleOpDal.Update(ScheduleOpDto.FromModel(model)),
                onNone: () => _scheduleOpDal.Insert(ScheduleOpDto.FromModel(model)));

        var listPpa = model.ListPpa.Select(x => ScheduleOpPpaDto.FromModel(model.ScheduleOpId, x)).ToList();
        _scheduleOpPpaDal.Delete(model);
        _scheduleOpPpaDal.Insert(listPpa);

        SaveHistoryDelta(model);
    }

    private void SaveHistoryDelta(ScheduleOpModel model)
    {
        if (!model.ListHistory.Any())
            return;

        var existingHist = _scheduleOpHistDal.ListData(model)?.ToList() ?? [];
        var existingNoUruts = existingHist.Select(x => x.NoUrut).ToHashSet();
        var newEntries = model.ListHistory
            .Where(x => !existingNoUruts.Contains(x.NoUrut))
            .Select(x => ScheduleOpHistDto.FromModel(model.ScheduleOpId, x))
            .ToList();

        if (newEntries.Count > 0)
        {
            _scheduleOpHistDal.Insert(newEntries);
        }
    }

    public MayBe<ScheduleOpModel> LoadEntity(IScheduleOpKey key)
    {   
        var dto = _scheduleOpDal.GetData(key);
        if (dto is null)
            return MayBe<ScheduleOpModel>.None;

        var listPpaDto = _scheduleOpPpaDal.ListData(key)?.ToList() ?? [];
        var listPpa = listPpaDto.Select(x => x.ToModel());

        var listHistDto = _scheduleOpHistDal.ListData(key)?.ToList() ?? [];
        var listHist = listHistDto.Select(x => x.ToModel());
        
        var model = dto.ToModel(listPpa, listHist);
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

    public IEnumerable<ScheduleOpView> ListData(IPasienKey filter)
    {
        var dto = _scheduleOpDal.ListData(filter)?.ToList() ?? [];
        return dto.Select(x => x.ToView());
    }

    public IEnumerable<ScheduleOpView> ListActiveSchedulesInRoom(string kamarId, DateTime date)
    {
        var dto = _scheduleOpDal.ListActiveSchedulesInRoom(kamarId, date)?.ToList() ?? [];
        return dto.Select(x => x.ToView());
    }

    public IEnumerable<ScheduleOpView> ListActiveSchedulesForDoctor(string ppaId, DateTime date)
    {
        var dto = _scheduleOpDal.ListActiveSchedulesForDoctor(ppaId, date)?.ToList() ?? [];
        return dto.Select(x => x.ToView());
    }
}
