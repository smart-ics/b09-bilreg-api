using Bilreg.Application.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public class PakaiBedRepo : IPakaiBedRepo
{
    private readonly IPakaiBedDal _dal;

    public PakaiBedRepo(IPakaiBedDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(PakaiBedModel model)
    {
        var dto = PakaiBedDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }

    public MayBe<PakaiBedModel> LoadEntity(IPakaiBedKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<PakaiBedModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<PakaiBedView> ListData(IIgdVisitKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());

    public MayBe<PakaiBedModel> LoadOpenForBed(IBedIgdKey bed)
    {
        var dto = _dal.GetOpenForBed(bed);
        return dto is null ? MayBe<PakaiBedModel>.None : MayBe.From(dto.ToModel());
    }

    public MayBe<PakaiBedModel> LoadOpenForVisit(IIgdVisitKey visit)
    {
        var dto = _dal.GetOpenForVisit(visit);
        return dto is null ? MayBe<PakaiBedModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<PakaiBedOrphanView> ListOrphans()
        => (_dal.ListOrphans()?.ToList() ?? []).Select(x => new PakaiBedOrphanView(
            x.PakaiBedId,
            x.IgdVisitId,
            x.BedIgdId,
            x.BedIgdName,
            x.CheckInDateTime,
            x.OrphanReason,
            x.VisitState,
            x.BedState,
            x.BedCurrentIgdVisitId));
}
