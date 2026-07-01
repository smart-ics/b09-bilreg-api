using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IBedRepo :
    ISaveChange<BedType>,
    ILoadEntity<BedType, IBedKey>,
    IDeleteEntity<IBedKey>,
    IListData<BedType>
{
    IEnumerable<BedType> ListData(IBangsalKey filter);
}