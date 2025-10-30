using Bilreg.Domain.BillContext.BedUsageFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.BedUsageFeature;

public interface IKelasRepo :
    ISaveChange<KelasType>,
    ILoadEntity<KelasType, IKelasKey>,
    IDeleteEntity<IKelasKey>,
    IListData<KelasType>
{
}