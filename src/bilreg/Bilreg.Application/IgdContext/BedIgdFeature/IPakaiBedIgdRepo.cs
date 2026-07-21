using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature;

public interface IPakaiBedIgdRepo :
    ISaveChange<PakaiBedIgdModel>,
    ILoadEntity<PakaiBedIgdModel, IPakaiBedIgdKey>,
    IListData<PakaiBedIgdView, IIgdVisitKey>
{
    MayBe<PakaiBedIgdModel> LoadOpenForBed(IBedIgdKey bed);
    MayBe<PakaiBedIgdModel> LoadOpenForVisit(IIgdVisitKey visit);
    IEnumerable<PakaiBedIgdOrphanView> ListOrphans();
}

public record PakaiBedIgdView(
    string PakaiBedIgdId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    DateTime CheckOutDateTime);

public record PakaiBedIgdOrphanView(
    string PakaiBedIgdId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    string OrphanReason,
    string VisitState,
    string BedState,
    string BedCurrentIgdVisitId);
