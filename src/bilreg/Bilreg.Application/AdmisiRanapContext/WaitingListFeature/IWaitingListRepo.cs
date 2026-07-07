using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature;

public interface IWaitingListRepo :
    ISaveChange<WaitingListModel>,
    ILoadEntity<WaitingListModel, IWaitingListKey>
{
    bool HasActiveByRegId(string regId);
}
