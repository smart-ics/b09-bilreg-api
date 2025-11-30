using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanFeature;

public interface IGroupKomponenRepo :
    ISaveChange<GroupKomponenType>,
    ILoadEntity<GroupKomponenType, IGroupKomponenKey>,
    IDeleteEntity<IGroupKomponenKey>,
    IListData<GroupKomponenType>
{
}