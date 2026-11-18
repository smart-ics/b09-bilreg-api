using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifBarangRepo :
    IListData<TarifBarangType, ITarifKey>
{
}
