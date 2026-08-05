using Farinv.Domain.SalesContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Farinv.Application.SalesContext.AntrianFeature;

public interface IAntrianRepo :
    ISaveChange<AntrianModel>,
    ILoadEntity<AntrianModel, IAntrianKey>,
    IDeleteEntity<IAntrianKey>,
    IListData<AntrianHeaderView, DateOnly>,
    IListData<AntrianView, DateTime>
{
    AntrianModel LoadOrCreate(int servicePoint);
}
