using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;

public interface IMapJaminanJkRepo :
    ILoadEntity<MapJaminanJkType, IJaminanKey>,
    IListData<MapJaminanJkType>
{
}
