using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.StandardFeature;

public interface IGolTerapiRepo :
    ISaveChange<GolTerapiType>,
    ILoadEntity<GolTerapiType, IGolTerapiKey>,
    IDeleteEntity<IGolTerapiKey>,
    IListData<GolTerapiType>
{
}
