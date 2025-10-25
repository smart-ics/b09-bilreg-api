using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IQuickSearchPasienDal : 
    IListDataMayBe<SearchPasienType, Periode>
{
}
