using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public interface ITipeTarifDal :
    IInsert<TipeTarifModel>,
    IUpdate<TipeTarifModel>,
    IDelete<ITipeTarifKey>,
    IGetData<TipeTarifModel, ITipeTarifKey>,
    IListData<TipeTarifModel>

{
}