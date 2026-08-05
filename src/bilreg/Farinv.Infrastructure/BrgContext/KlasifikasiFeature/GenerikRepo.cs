using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class GenerikRepo : IGenerikRepo
{
    private readonly IGenerikDal _generikDal;
    public GenerikRepo(IGenerikDal generikDal)
    {
        _generikDal = generikDal;
    }
    
    public void SaveChanges(GenerikType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _generikDal.Update(GenerikDto.FromModel(model)),
                onNone: () => _generikDal.Insert(GenerikDto.FromModel(model)));
    }
    
    public MayBe<GenerikType> LoadEntity(IGenerikKey key)
    {   
        var dto = _generikDal.GetData(key);
        if (dto is null)
            return MayBe<GenerikType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }
    
    public void DeleteEntity(IGenerikKey key)
    {
        _generikDal.Delete(key);
    }
    
    public IEnumerable<GenerikType> ListData()
    {
        var listDto = _generikDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}