using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public class JenisInapRepo : IJenisInapRepo
{
    private readonly IJenisInapDal _jenisInapDal;

    public JenisInapRepo(IJenisInapDal jenisInapDal)
    {
        _jenisInapDal = jenisInapDal;
    }

    public MayBe<JenisInapType> LoadEntity(IJenisInapKey key)
    {
        var dto = _jenisInapDal.GetData(key);
        if (dto is null)
            return MayBe<JenisInapType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public IEnumerable<JenisInapType> ListData()
    {
        var listDto = _jenisInapDal.ListData() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}
