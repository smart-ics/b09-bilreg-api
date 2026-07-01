using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub;

public class RujukanRepo : IRujukanRepo
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanRepo(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }

    public MayBe<RujukanType> LoadEntity(IRujukanKey key)
    {
        var dto = _rujukanDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public IEnumerable<RujukanType> ListData(ITipeRujukanKey filter)
    {
        var result = _rujukanDal.ListData(filter);
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }

    public IEnumerable<RujukanType> ListData(ICaraMasukDkKey caraMasuk)
    {
        var result = _rujukanDal.ListData(caraMasuk);
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }

    public MayBe<RujukanType> LoadEntity(IPpkKey ppkkey)
    {
        var dto = _rujukanDal.GetDataByPpkId(ppkkey);
        if (dto is null)
            return MayBe<RujukanType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }
}