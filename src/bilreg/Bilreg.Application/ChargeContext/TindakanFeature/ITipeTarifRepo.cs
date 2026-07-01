using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface ITipeTarifRepo :
    ISaveChange<TipeTarifType>,
    ILoadEntity<TipeTarifType, ITipeTarifKey>,
    IDeleteEntity<ITipeTarifKey>,
    IListData<TipeTarifType>
{
}
