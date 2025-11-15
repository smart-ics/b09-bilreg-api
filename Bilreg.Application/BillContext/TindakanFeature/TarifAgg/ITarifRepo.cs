using Bilreg.Domain.BillContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.TarifAgg;

public interface ITarifRepo :
    ILoadEntity<TarifType, ITarifKey>,
    IListData<TarifType>,
    IListData<TarifType, string>
{
}
