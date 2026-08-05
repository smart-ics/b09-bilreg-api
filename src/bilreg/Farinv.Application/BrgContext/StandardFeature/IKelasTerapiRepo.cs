using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.StandardFeature;

public interface IKelasTerapiRepo :
    ISaveChange<KelasTerapiType>,
    ILoadEntity<KelasTerapiType, IKelasTerapiKey>,
    IDeleteEntity<IKelasTerapiKey>,
    IListData<KelasTerapiType>
{
}
