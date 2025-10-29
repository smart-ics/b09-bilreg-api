using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IDeepSearchPasienDal :
    IListDataMayBe<SearchPasienType, IEnumerable<SearchPasienType>>
    
{
}
