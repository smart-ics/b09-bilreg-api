using Farinv.Application.InventoryContext.StokFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public class LayananRepo : ILayananRepo
{
    private readonly ILayananDal _layananDal;
    public LayananRepo(ILayananDal layananDal)
    {
        _layananDal = layananDal;
    }
    
    public MayBe<LayananType> LoadEntity(ILayananKey key)
    {   
        var dto = _layananDal.GetData(key);
        if (dto is null)
            return MayBe<LayananType>.None;

        var model = dto.ToModel();
        return MayBe.From(model);
    }
    
    public IEnumerable<LayananType> ListData()
    {
        var listDto = _layananDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        
        return result;
    }
}