using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class AntrianMapRepo : IAntrianMapRepo
{
    private readonly IAntrianMapDal _antrianMapDal;

    public AntrianMapRepo(IAntrianMapDal antrianMapDal)
    {
        _antrianMapDal = antrianMapDal;
    }

    public void SaveChanges(AntrianMapModel model)
    {
       _antrianMapDal.Delete(model);
       _antrianMapDal.Insert(AntrianMapDto.FromModel(model));
       
    }

    public MayBe<AntrianMapModel> LoadEntity(IAntrianMapKey key)
    {
        var antrianMap = _antrianMapDal.ListData(key)?.ToList() ?? [];
        var first = antrianMap.FirstOrDefault();
        var model = first?.ToModel(antrianMap);
        
        return MayBe.From(model!);
    }

}
