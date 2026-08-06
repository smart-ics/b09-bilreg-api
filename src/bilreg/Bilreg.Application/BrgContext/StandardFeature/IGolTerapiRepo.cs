using Bilreg.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.StandardFeature;

public interface IGolTerapiRepo :
    ISaveChange<GolTerapiType>,
    ILoadEntity<GolTerapiType, IGolTerapiKey>,
    IDeleteEntity<IGolTerapiKey>,
    IListData<GolTerapiType>
{
}
