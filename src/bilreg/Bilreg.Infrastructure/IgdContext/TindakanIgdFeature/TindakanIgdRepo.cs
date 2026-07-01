using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public class TindakanIgdRepo : ITindakanIgdRepo
{
    private readonly ITindakanIgdDal _dal;

    public TindakanIgdRepo(ITindakanIgdDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(TindakanIgdModel model)
    {
        var dto = TindakanIgdDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }
    public void DeleteEntity(ITindakanIgdKey key)
    {
        _dal.Delete(key);
    }
    public MayBe<TindakanIgdModel> LoadEntity(ITindakanIgdKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<TindakanIgdModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<TindakanIgdView> ListData(IIgdVisitKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());

    public bool AnyForVisit(IIgdVisitKey visit) => _dal.CountForVisit(visit) > 0;

    
}
