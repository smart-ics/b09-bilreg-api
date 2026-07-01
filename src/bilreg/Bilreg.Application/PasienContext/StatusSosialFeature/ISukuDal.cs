using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public interface ISukuDal :
    IInsert<SukuType>,
    IUpdate<SukuType>,
    IDelete<ISukuKey>,
    IGetDataMayBe<SukuType, ISukuKey>,
    IListDataMayBe<SukuType>
{
}