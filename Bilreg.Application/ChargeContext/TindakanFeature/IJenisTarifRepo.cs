using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface IJenisTarifRepo :
    ISaveChange<JenisTarifType>,
    ILoadEntity<JenisTarifType, IJenisTarifKey>,
    IDeleteEntity<IJenisTarifKey>,
    IListData<JenisTarifType>
{
}