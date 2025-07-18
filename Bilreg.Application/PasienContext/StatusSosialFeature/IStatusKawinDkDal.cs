using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public interface IStatusKawinDkDal :
    IInsert<StatusKawinDkType>,
    IUpdate<StatusKawinDkType>,
    IDelete<IStatusKawinDkKey>,
    IGetDataMayBe<StatusKawinDkType, IStatusKawinDkKey>,
    IListDataMayBe<StatusKawinDkType>
{
}