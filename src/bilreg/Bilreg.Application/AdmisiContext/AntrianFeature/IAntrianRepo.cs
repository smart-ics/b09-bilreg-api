using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAntrianRepo :
    ISaveChange<AntrianModel>,
    ILoadEntity<AntrianModel, IAntrianKey>,
    IDeleteEntity<IAntrianKey>,
    IListData<AntrianHeaderView, DateOnly>,
    IListData<AntrianView, DateTime>
{
    void FixOutstandingReference();
    bool TrySaveAnonymousInServiceTransition(AntrianModel queue, AntrianEntryModel entry);
    bool TrySaveWaitingToInServiceTransition(AntrianModel queue, AntrianEntryModel entry);
    bool TrySaveInServiceToDoneTransition(AntrianModel queue, AntrianEntryModel entry);
    void SaveNewEntry(AntrianModel queue, AntrianEntryModel entry);
}
