using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public interface IDeepSearchPasienDal :
    IListDataMayBe<SearchPasienModel, string>,
    IListDataMayBe<SearchPasienModel, IEnumerable<string>>

    
{
}
