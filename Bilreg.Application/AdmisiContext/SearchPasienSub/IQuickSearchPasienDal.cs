using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public interface IQuickSearchPasienDal : 
    IListDataMayBe<SearchPasienType, Periode>
{
}
