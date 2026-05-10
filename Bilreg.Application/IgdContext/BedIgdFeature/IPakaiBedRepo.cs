using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature;

public interface IPakaiBedRepo :
    ISaveChange<PakaiBedModel>,
    ILoadEntity<PakaiBedModel, IPakaiBedKey>,
    IListData<PakaiBedView, IIgdVisitKey>
{
    MayBe<PakaiBedModel> LoadOpenForBed(IBedIgdKey bed);
    MayBe<PakaiBedModel> LoadOpenForVisit(IIgdVisitKey visit);
    IEnumerable<PakaiBedOrphanView> ListOrphans();
}

public record PakaiBedView(
    string PakaiBedId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    DateTime CheckOutDateTime);

public record PakaiBedOrphanView(
    string PakaiBedId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    string OrphanReason,
    string VisitState,
    string BedState,
    string BedCurrentIgdVisitId);
