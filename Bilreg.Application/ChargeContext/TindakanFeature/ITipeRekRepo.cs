using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface ITipeRekRepo :
    ISaveChange<TipeRekType>,
    ILoadEntity<TipeRekType, ITipeRekKey>,
    IDeleteEntity<ITipeRekKey>,
    IListData<TipeRekType>
{
}