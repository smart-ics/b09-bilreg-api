using Bilreg.Application.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.SearchPasienSub;

public class QuickSearchPasienDal : IQuickSearchPasienDal
{
    public MayBe<IEnumerable<SearchPasienType>> ListData(Periode filter)
    {
        throw new NotImplementedException();
    }
}
