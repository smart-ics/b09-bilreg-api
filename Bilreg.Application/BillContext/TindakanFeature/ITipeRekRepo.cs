using Bilreg.Domain.BillContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanFeature;

public interface ITipeRekRepo :
    ISaveChange<TipeRekType>,
    ILoadEntity<TipeRekType, ITipeRekKey>,
    IDeleteEntity<ITipeRekKey>,
    IListData<TipeRekType>
{
}