using Farinv.Application.InventoryContext.StokFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public class JenisLokasiRepo : IJenisLokasiRepo
{
    private readonly IJenisLokasiDal _jenisLokasiDal;
    public JenisLokasiRepo(IJenisLokasiDal jenisLokasiDal)
    {
        _jenisLokasiDal = jenisLokasiDal;
    }
    
    public void SaveChanges(JenisLokasiType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _jenisLokasiDal.Update(model),
                onNone: () => _jenisLokasiDal.Insert(model));
    }
    
    public MayBe<JenisLokasiType> LoadEntity(IJenisLokasiKey key)
    {   
        var dto = _jenisLokasiDal.GetData(key);
        if (dto is null)
            return MayBe<JenisLokasiType>.None;
        var model = dto;
        return MayBe.From(model);
    }
    
    public void DeleteEntity(IJenisLokasiKey key)
    {
        _jenisLokasiDal.Delete(key);
    }
    
    public IEnumerable<JenisLokasiType> ListData()
    {
        var listDto = _jenisLokasiDal.ListData()?.ToList() ?? [];
        var result = listDto;
        return result;
    }
}