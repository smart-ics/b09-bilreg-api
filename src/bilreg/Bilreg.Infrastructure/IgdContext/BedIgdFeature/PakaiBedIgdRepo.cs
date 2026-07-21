using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public class PakaiBedIgdRepo : IPakaiBedIgdRepo
{
    private readonly IPakaiBedIgdDal _dal;

    public PakaiBedIgdRepo(IPakaiBedIgdDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(PakaiBedIgdModel model)
    {
        var dto = PakaiBedIgdDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }

    public MayBe<PakaiBedIgdModel> LoadEntity(IPakaiBedIgdKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<PakaiBedIgdModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<PakaiBedIgdView> ListData(IIgdVisitKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());

    public MayBe<PakaiBedIgdModel> LoadOpenForBed(IBedIgdKey bed)
    {
        var dto = _dal.GetOpenForBed(bed);
        return dto is null ? MayBe<PakaiBedIgdModel>.None : MayBe.From(dto.ToModel());
    }

    public MayBe<PakaiBedIgdModel> LoadOpenForVisit(IIgdVisitKey visit)
    {
        var dto = _dal.GetOpenForVisit(visit);
        return dto is null ? MayBe<PakaiBedIgdModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<PakaiBedIgdOrphanView> ListOrphans()
        => (_dal.ListOrphans()?.ToList() ?? []).Select(x => new PakaiBedIgdOrphanView(
            x.PakaiBedIgdId,
            x.IgdVisitId,
            x.BedIgdId,
            x.BedIgdName,
            x.CheckInDateTime,
            x.OrphanReason,
            x.VisitState,
            x.BedState,
            x.BedCurrentIgdVisitId));
}
