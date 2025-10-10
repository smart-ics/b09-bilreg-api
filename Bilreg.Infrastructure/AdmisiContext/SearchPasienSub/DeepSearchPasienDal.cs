using Bilreg.Application.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.SearchPasienSub;

public class DeepSearchPasienDal : IDeepSearchPasienDal
{
    public MayBe<IEnumerable<SearchPasienModel>> ListData(string filter)
    {
        throw new NotImplementedException();
    }
}
