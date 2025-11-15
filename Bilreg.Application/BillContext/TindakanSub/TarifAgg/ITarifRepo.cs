using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.TarifAgg;

public interface ITarifRepo :
    ILoadEntity<TarifType, ITarifKey>,
    IListData<TarifType>,
    IListData<TarifType, string>
{
}
