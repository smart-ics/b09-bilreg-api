using Bilreg.Domain.AccountingContext.CoaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AccountingContext.JurnalFeature;

public interface ICoaRepo :
    ILoadEntity<CoaType, ICoaKey>,
    IListData<CoaType>
{
}
