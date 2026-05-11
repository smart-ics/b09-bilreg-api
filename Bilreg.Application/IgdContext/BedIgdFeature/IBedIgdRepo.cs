using Bilreg.Domain.IgdContext.BedIgdFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BedIgdFeature;

public interface IBedIgdRepo :
    ISaveChange<BedIgdModel>,
    ILoadEntity<BedIgdModel, IBedIgdKey>,
    IDeleteEntity<IBedIgdKey>,
    IListData<BedIgdView>
{
    IEnumerable<BedIgdView> ListAvailable();
}

public record BedIgdView(
    string BedIgdId,
    string BedIgdName,
    string KamarName,
    string BedState,
    string CurrentIgdVisitId,
    DateTime OccupyDateTime);
