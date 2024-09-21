using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.TarifAgg;

public interface ITarifDal:
    IInsert<TarifModel>,
    IUpdate<TarifModel>,
    IDelete<TarifModel>,
    IGetData<TarifModel, ITarifKey>,
    IListData<TarifModel>
{
}