using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public class IgdVisitRepo : IIgdVisitRepo
{
    private readonly IIgdVisitDal _visitDal;
    private readonly IIgdVisitEventDal _eventDal;
    private readonly IIgdVisitTriageDal _triageDal;

    public IgdVisitRepo(
        IIgdVisitDal visitDal,
        IIgdVisitEventDal eventDal,
        IIgdVisitTriageDal triageDal)
    {
        _visitDal = visitDal;
        _eventDal = eventDal;
        _triageDal = triageDal;
    }

    public void SaveChanges(IgdVisitModel model)
    {
        var dto = IgdVisitDto.FromModel(model);

        LoadEntity(model)
            .Match(
                onSome: _ => _visitDal.Update(dto),
                onNone: () => _visitDal.Insert(dto));

        _eventDal.Delete(model);
        foreach (var evt in model.ListEvent)
            _eventDal.Insert(IgdVisitEventDto.FromModel(model.IgdVisitId, evt));

        _triageDal.Delete(model);
        foreach (var triage in model.ListTriage)
            _triageDal.Insert(IgdVisitTriageDto.FromModel(model.IgdVisitId, triage));
    }

    public MayBe<IgdVisitModel> LoadEntity(IIgdVisitKey key)
    {
        var dto = _visitDal.GetData(key);
        if (dto is null)
            return MayBe<IgdVisitModel>.None;

        var triages = _triageDal.ListData(key)?.Select(x => x.ToModel()) ?? [];
        var events = _eventDal.ListData(key)?.Select(x => x.ToModel()) ?? [];
        var model = dto.ToModel(triages, events);
        return MayBe.From(model);
    }

    public void DeleteEntity(IIgdVisitKey key)
    {
        _eventDal.Delete(key);
        _triageDal.Delete(key);
        _visitDal.Delete(key);
    }

    public IEnumerable<IgdVisitView> ListData(Periode periode)
    {
        var listDto = _visitDal.ListData(periode)?.ToList() ?? [];
        return listDto.Select(x => x.ToView());
    }

    public IEnumerable<IgdVisitView> ListAktif()
    {
        var listDto = _visitDal.ListAktif()?.ToList() ?? [];
        return listDto.Select(x => x.ToView());
    }

    public MayBe<IgdVisitView> GetByRegId(string regId)
    {
        var dto = _visitDal.GetByRegId(regId);
        if (dto is null)
            return MayBe<IgdVisitView>.None;
        var data = dto.ToView();
        return MayBe.From(data);
    }
}
