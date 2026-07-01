using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.BrgFeature;

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
        switch (model)
        {
            case BrgObatType brgObat:
                _brgDal.Update(BrgDto.FromModel(brgObat));
                break;
            case BrgBhpType bhp:
                _brgDal.Update(BrgDto.FromModel(bhp));
                break;
        }
    }
    
    private void BrgInsert(IBrg model)
    {
        switch (model)
        {
            case BrgObatType brgObat:
                _brgDal.Insert(BrgDto.FromModel(brgObat));
                break;
            case BrgBhpType bhp:
                _brgDal.Insert(BrgDto.FromModel(bhp));
                break;
        }
    }
    
    public MayBe<IBrg> LoadEntity(IBrgKey key)
    {   
        var dto = _brgDal.GetData(key);
        if (dto is null)
            return MayBe<IBrg>.None;

        var listSatuanDto = _brgSatuanDal.ListData(key)?.ToList() ?? [];
        var listSatuan = listSatuanDto.Select(x => x.ToModel());
        
        IBrg result = dto.fs_kd_grup_rek_dk switch
        {
            "OBT" => dto.ToModelObat(listSatuan),
            "BHP" => dto.ToModelBhp(listSatuan),
            _ => throw new ArgumentOutOfRangeException()
        };
        return MayBe.From(result);
    }
    
    public void DeleteEntity(IBrgKey key)
    {
        _brgDal.Delete(key);
    }
    
    public IEnumerable<BrgView> ListData(string keyword)
    {
        var listView = _brgDal.ListData(keyword)?.ToList() ?? [];
        return listView;
    }
}