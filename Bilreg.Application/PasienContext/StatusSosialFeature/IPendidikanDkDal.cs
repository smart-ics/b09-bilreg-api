using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialFeature.PendidikanDkAgg;

public interface IPendidikanDkDal :
    IInsert<PendidikanDkType>,
    IUpdate<PendidikanDkType>,
    IDelete<IPendidikanDkKey>,
    IGetDataMayBe<PendidikanDkType, IPendidikanDkKey>,
    IListDataMayBe<PendidikanDkType>
{
}