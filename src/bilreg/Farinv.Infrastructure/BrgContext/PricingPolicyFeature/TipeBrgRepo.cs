using Farinv.Application.BrgContext.PricingPolicyFeature;
using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.PricingPolicyFeature;

public class TipeBrgRepo : ITipeBrgRepo
{
    private readonly ITipeBrgDal _tipeBrgDal;

    public TipeBrgRepo(ITipeBrgDal tipeBrgDal)
    {
        _tipeBrgDal = tipeBrgDal;
    }

    public void SaveChanges(TipeBrgType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tipeBrgDal.Update(TipeBrgDto.FromModel(model)),
                onNone: () => _tipeBrgDal.Insert(TipeBrgDto.FromModel(model)));
    }

    public MayBe<TipeBrgType> LoadEntity(ITipeBrgKey key)
    {
        var dto = _tipeBrgDal.GetData(key);
        if (dto is null)
            return MayBe<TipeBrgType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(ITipeBrgKey key)
    {
        _tipeBrgDal.Delete(key);
    }

    public IEnumerable<TipeBrgType> ListData()
    {
        var listDto = _tipeBrgDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}