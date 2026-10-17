using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature;

public interface IWaitingListRepo :
    ISaveChange<WaitingListModel>,
    ILoadEntity<WaitingListModel, IWaitingListKey>
{
    void Save(WaitingListModel model);
    bool HasActiveByRegId(string regId);
    MayBe<WaitingListModel> LoadActiveByRegId(string regId);
    IEnumerable<WaitingListModel> ListByRegId(string regId);
}
