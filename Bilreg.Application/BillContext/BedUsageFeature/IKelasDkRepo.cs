using Bilreg.Domain.BillContext.BedUsageFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.BedUsageFeature;

public interface IKelasDkRepo :
    ILoadEntity<KelasDkType, IKelasDkKey>,
    IListData<KelasDkType>
{
}