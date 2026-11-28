using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceRepo : IPreOpClearanceRepo
{
    private readonly IPreOpClearanceDal _clearanceDal;
    private readonly IPreOpClearanceChecklistDal _checklistDal;

    public PreOpClearanceRepo(
        IPreOpClearanceDal clearanceDal,
        IPreOpClearanceChecklistDal checklistDal)
    {
        _clearanceDal = clearanceDal;
        _checklistDal = checklistDal;
    }

    public MayBe<PreOpClearanceModel> GetLatestByScheduleOpId(string scheduleOpId)
    {
        var dto = _clearanceDal.GetLatestByScheduleOpId(scheduleOpId);
        if (dto is null)
            return MayBe<PreOpClearanceModel>.None;

        var listItemDto = _checklistDal.ListData(dto.ClearanceId)?.ToList() ?? [];
        var items = listItemDto.Select(x => x.ToModel()).ToList();

        return MayBe.From(dto.ToModel(items));
    }

    public int CountByScheduleOpId(string scheduleOpId)
    {
        return _clearanceDal.CountByScheduleOpId(scheduleOpId);
    }

    public void SaveChanges(PreOpClearanceModel model)
    {
        using var trans = TransHelper.NewScope();

        var existing = _clearanceDal.GetData(model.ClearanceId);
        var dto = PreOpClearanceDto.FromModel(model);
        if (existing is null)
            _clearanceDal.Insert(dto);
        else
            _clearanceDal.Update(dto);

        var listChecklist = model.ChecklistItems
            .Select((item, index) => PreOpClearanceChecklistDto.FromModel(model.ClearanceId, index + 1, item))
            .ToList();

        _checklistDal.Delete(model.ClearanceId);
        if (listChecklist.Count > 0)
            _checklistDal.Insert(listChecklist);

        trans.Complete();
    }
}
