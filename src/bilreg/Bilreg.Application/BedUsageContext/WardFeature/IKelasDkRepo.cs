using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IKelasDkRepo :
    ILoadEntity<KelasDkType, IKelasDkKey>,
    IListData<KelasDkType>
{
}