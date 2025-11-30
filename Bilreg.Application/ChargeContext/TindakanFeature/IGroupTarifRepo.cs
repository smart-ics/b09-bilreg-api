using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanFeature;

public interface IGroupTarifRepo :
    ISaveChange<GroupTarifType>,
    ILoadEntity<GroupTarifType, IGroupTarifKey>,
    IDeleteEntity<IGroupTarifKey>,
    IListData<GroupTarifType>
{
}