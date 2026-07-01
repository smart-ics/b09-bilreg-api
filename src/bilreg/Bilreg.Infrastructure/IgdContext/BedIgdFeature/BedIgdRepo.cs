using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public class BedIgdRepo : IBedIgdRepo
{
    private readonly IBedIgdDal _bedDal;

    public BedIgdRepo(IBedIgdDal bedDal)
    {
        _bedDal = bedDal;
    }

    public void SaveChanges(BedIgdModel model)
    {
        var dto = BedIgdDto.FromModel(model);

        var existing = _bedDal.GetData(model);
        if (existing is null)
        {
            _bedDal.Insert(dto);
            return;
        }

        if (NeedsConditionalUpdate(model))
        {
            var priorState = model.BedStateSnapshot.ToCode();
            var priorVisit = model.CurrentIgdVisitIdSnapshot == "-" ? "" : model.CurrentIgdVisitIdSnapshot;
            var rows = _bedDal.UpdateOccupancyConditional(dto, priorState, priorVisit);
            if (rows == 0)
                throw new InvalidOperationException(
                    $"Bed {model.BedIgdId} occupancy stale; please reload (expected state: {priorState}, expected visit: '{priorVisit}').");
            return;
        }

        _bedDal.Update(dto);
    }

    public MayBe<BedIgdModel> LoadEntity(IBedIgdKey key)
    {
        var dto = _bedDal.GetData(key);
        return dto is null ? MayBe<BedIgdModel>.None : MayBe.From(dto.ToModel());
    }

    public void DeleteEntity(IBedIgdKey key) => _bedDal.Delete(key);

    public IEnumerable<BedIgdView> ListData()
        => (_bedDal.ListData()?.ToList() ?? []).Select(x => x.ToView());

    public IEnumerable<BedIgdView> ListAvailable()
        => (_bedDal.ListAvailable()?.ToList() ?? []).Select(x => x.ToView());

    private static bool NeedsConditionalUpdate(BedIgdModel model)
    {
        var stateChanged = model.BedState != model.BedStateSnapshot;
        var visitChanged = model.CurrentIgdVisitId != model.CurrentIgdVisitIdSnapshot;
        return stateChanged || visitChanged;
    }
}
