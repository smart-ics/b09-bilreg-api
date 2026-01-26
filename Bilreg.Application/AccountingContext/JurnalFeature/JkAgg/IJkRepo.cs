using Bilreg.Domain.AccountingContext.UnitFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;

public interface IJkRepo :
    ILoadEntity<JkType, IJkKey>,
    IListData<JkType>
{
}
