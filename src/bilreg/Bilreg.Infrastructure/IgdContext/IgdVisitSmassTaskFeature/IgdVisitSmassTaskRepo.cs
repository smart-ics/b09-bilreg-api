using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitSmassTaskFeature;

public class IgdVisitSmassTaskRepo : IIgdVisitSmassTaskRepo
{
    private readonly IIgdVisitSmassTaskDal _dal;

    public IgdVisitSmassTaskRepo(IIgdVisitSmassTaskDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(IgdVisitSmassTaskModel model)
    {
        var dto = IgdVisitSmassTaskDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<IgdVisitSmassTaskModel> LoadEntity(IIgdVisitSmassTaskKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null
            ? MayBe<IgdVisitSmassTaskModel>.None
            : MayBe.From(dto.ToModel());
    }

    public MayBe<IgdVisitSmassTaskModel> FindByBusinessKey(
        string igdVisitId,
        int noTriage,
        SmassTaskTypeEnum taskType)
    {
        var dto = _dal.FindByBusinessKey(igdVisitId, noTriage, taskType);
        return dto is null
            ? MayBe<IgdVisitSmassTaskModel>.None
            : MayBe.From(dto.ToModel());
    }

    public IEnumerable<IgdVisitSmassTaskModel> ListByVisit(string igdVisitId)
        => _dal.ListByVisit(igdVisitId).Select(x => x.ToModel()).ToList();

    public IEnumerable<IgdVisitSmassTaskModel> ListProcessable()
        => _dal.ListProcessable().Select(x => x.ToModel()).ToList();
}
