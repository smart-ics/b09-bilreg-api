using Bilreg.Domain.AccountingContext.UnitFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AccountingContext.JurnalFeature.UnitAgg;

public interface IUnitRepo :
    ILoadEntity<UnitType, IUnitKey>,
    IListData<UnitType>
{
}
