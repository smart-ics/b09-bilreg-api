using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public interface IPekerjaanDkDal :
    IInsert<PekerjaanDkType>,
    IUpdate<PekerjaanDkType>,
    IDelete<IPekerjaanDkKey>,
    IGetDataMayBe<PekerjaanDkType, IPekerjaanDkKey>,
    IListDataMayBe<PekerjaanDkType>
{
}