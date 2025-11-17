using Bilreg.Domain.BillContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanFeature;

public interface ITipeTarifRepo :
    ISaveChange<TipeTarifType>,
    ILoadEntity<TipeTarifType, ITipeTarifKey>,
    IDeleteEntity<ITipeTarifKey>,
    IListData<TipeTarifType>
{
}
