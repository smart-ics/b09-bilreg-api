using Bilreg.Domain.AccountingContext.UnitFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AccountingContext.JurnalFeature.UnitGrupAgg;

public interface IUnitGrupRepo :
    ILoadEntity<UnitGrupType, IUnitGrupKey>,
    IListData<UnitGrupType>
{
}
