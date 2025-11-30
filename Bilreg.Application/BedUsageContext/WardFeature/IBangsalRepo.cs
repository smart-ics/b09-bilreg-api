using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IBangsalRepo :
    ISaveChange<BangsalType>,
    ILoadEntity<BangsalType, IBangsalKey>,
    IDeleteEntity<IBangsalKey>,
    IListData<BangsalType>
{
}