using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface IGroupKomponenRepo :
    ISaveChange<GroupKomponenType>,
    ILoadEntity<GroupKomponenType, IGroupKomponenKey>,
    IDeleteEntity<IGroupKomponenKey>,
    IListData<GroupKomponenType>
{
}