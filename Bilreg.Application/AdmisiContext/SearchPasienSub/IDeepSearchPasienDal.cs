using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.SearchPasienSub;

public interface IDeepSearchPasienDal :
    //IListDataMayBe<SearchPasienType, string>,
    //IListDataMayBe<SearchPasienType, IEnumerable<string>>,
    IListDataMayBe<SearchPasienType, IEnumerable<SearchPasienType>>
    
{
}
