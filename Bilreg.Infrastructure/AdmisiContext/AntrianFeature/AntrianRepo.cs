using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class AntrianRepo : IAntrianRepo
{
    public void SaveChanges(AntrianModel model)
    {
        throw new NotImplementedException();
    }

    public MayBe<AntrianModel> LoadEntity(IAntrianKey key)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IAntrianHeaderView> ListData(DateOnly filter)
    {
        throw new NotImplementedException();
    }
}