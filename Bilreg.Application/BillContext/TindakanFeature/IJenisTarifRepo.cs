using Bilreg.Domain.BillContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanFeature;

public interface IJenisTarifRepo :
    ISaveChange<JenisTarifType>,
    ILoadEntity<JenisTarifType, IJenisTarifKey>,
    IDeleteEntity<IJenisTarifKey>,
    IListData<JenisTarifType>
{
}