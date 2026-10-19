using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BrgContext.BrgFeature;

public class BrgRepo : IBrgRepo
{
    private readonly IBrgDal _brgDal;
    private readonly IBrgSatuanDal _brgSatuanDal;
    
    public BrgRepo(IBrgDal brgDal, 
        IBrgSatuanDal brgSatuanDal)
    {
        _brgDal = brgDal;
        _brgSatuanDal = brgSatuanDal;
    }
    
    public void SaveChanges(IBrg model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => BrgUpdate(model),
                onNone: () => BrgInsert(model));
        _brgSatuanDal.Delete(model);
        _brgSatuanDal.Insert(model.ListSatuan
            .Select(x => BrgSatuanDto.FromModel(model.BrgId, x))
            .ToList());
    }
    
    private void BrgUpdate(IBrg model)
    {
        _brgDal.Update(BrgDto.FromModel(model));
    }
    
    private void BrgInsert(IBrg model)
    {
        _brgDal.Insert(BrgDto.FromModel(model));
    }
    
    public MayBe<IBrg> LoadEntity(IBrgKey key)
    {   
        var dto = _brgDal.GetData(key);
        if (dto is null)
            return MayBe<IBrg>.None;

        var listSatuanDto = _brgSatuanDal.ListData(key)?.ToList() ?? [];
        var listSatuan = listSatuanDto.Select(x => x.ToModel());
        
        IBrg result = dto.ToModel(listSatuan);
        return MayBe.From(result);
    }
    
    public void DeleteEntity(IBrgKey key)
    {
        _brgSatuanDal.Delete(key);
        _brgDal.Delete(key);
    }
    
    public IEnumerable<BrgView> ListData(string keyword)
    {
        var listView = _brgDal.ListData(keyword)?.ToList() ?? [];
        return listView;
    }
}