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
}