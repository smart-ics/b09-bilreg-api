using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.BhpIgdFeature;

public class BhpIgdRepo : IBhpIgdRepo
{
    private readonly IBhpIgdDal _dal;

    public BhpIgdRepo(IBhpIgdDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(BhpIgdModel model)
    {
        var dto = BhpIgdDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }

    public MayBe<BhpIgdModel> LoadEntity(IBhpIgdKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<BhpIgdModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<BhpIgdView> ListData(IIgdVisitKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());

    public bool AnyForVisit(IIgdVisitKey visit) => _dal.CountForVisit(visit) > 0;
}
