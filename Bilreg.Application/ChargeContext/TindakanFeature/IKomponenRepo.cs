using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface IKomponenRepo :
    ISaveChange<KomponenType>,
    ILoadEntity<KomponenType, IKomponenKey>,
    IDeleteEntity<IKomponenKey>,
    IListData<KomponenType>
{
}