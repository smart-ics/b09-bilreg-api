using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IKelasRepo :
    ISaveChange<KelasType>,
    ILoadEntity<KelasType, IKelasKey>,
    IDeleteEntity<IKelasKey>,
    IListData<KelasType>
{
}