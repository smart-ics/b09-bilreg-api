using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface IGroupTarifRepo :
    ISaveChange<GroupTarifType>,
    ILoadEntity<GroupTarifType, IGroupTarifKey>,
    IDeleteEntity<IGroupTarifKey>,
    IListData<GroupTarifType>
{
}