using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanFeature;

public interface IGroupTarifDkRepo :
    ISaveChange<GroupTarifDkType>,
    ILoadEntity<GroupTarifDkType, IGroupTarifDkKey>,
    IDeleteEntity<IGroupTarifDkKey>,
    IListData<GroupTarifDkType>
{
}