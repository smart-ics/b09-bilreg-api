using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Nuna.Lib.PatternHelper;
using System.Collections;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public class RuangRepo : IRuangRepo
{
    private readonly IRuangDal _ruangDal;

    public RuangRepo(IRuangDal ruangDal)
    {
        _ruangDal = ruangDal;
    }

    public void SaveChanges(RuangType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _ruangDal.Update(RuangDto.FromModel(model)),
                onNone: () => _ruangDal.Insert(RuangDto.FromModel(model))
            );
            
    }

    public MayBe<RuangType> LoadEntity(IRuangKey key)
    {
        var result = _ruangDal.GetData(key);
        if(result is null) 
            return MayBe<RuangType>.None;
        var model = result.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IRuangKey key)
        => _ruangDal.Delete(key);

    public IEnumerable<RuangType> ListData()
    {
        var result = _ruangDal.ListData();
        var model = result?.Select(x => x.ToModel())?
            .ToList() ?? [];
        return model;
    }
}
