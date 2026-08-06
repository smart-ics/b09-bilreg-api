using Bilreg.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.StandardFeature;

public interface IKelasTerapiRepo :
    ISaveChange<KelasTerapiType>,
    ILoadEntity<KelasTerapiType, IKelasTerapiKey>,
    IDeleteEntity<IKelasTerapiKey>,
    IListData<KelasTerapiType>
{
}
