using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public interface IKotaDal :
    IInsert<KotaType>,
    IUpdate<KotaType>,
    IDelete<IKotaKey>,
    IGetDataMayBe<KotaType, IKotaKey>,
    IListDataMayBe<KotaType>
{
}