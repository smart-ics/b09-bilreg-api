using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifRepo :
    ILoadEntity<TarifType, ITarifKey>,
    IListData<TarifType>,
    IListData<TarifType, string>
{
}
