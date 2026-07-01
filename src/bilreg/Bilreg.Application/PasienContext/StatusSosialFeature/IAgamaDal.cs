using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public interface IAgamaDal :
    IInsert<AgamaType>,
    IUpdate<AgamaType>,
    IDelete<IAgamaKey>,
    IGetDataMayBe<AgamaType, IAgamaKey>,
    IListDataMayBe<AgamaType>
{
}