using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IBangsalRepo :
    ISaveChange<BangsalType>,
    ILoadEntity<BangsalType, IBangsalKey>,
    IDeleteEntity<IBangsalKey>,
    IListData<BangsalType>
{
    IEnumerable<BangsalType> ListData(ILayananKey filter);
}
