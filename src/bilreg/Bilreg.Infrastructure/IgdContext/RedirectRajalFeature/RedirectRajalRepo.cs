using Bilreg.Application.IgdContext.RedirectRajalFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.RedirectRajalFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.IgdContext.RedirectRajalFeature;

public class RedirectRajalRepo : IRedirectRajalRepo
{
    private readonly IRedirectRajalDal _dal;

    public RedirectRajalRepo(IRedirectRajalDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(RedirectRajalModel model)
    {
        var dto = RedirectRajalDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }

    public MayBe<RedirectRajalModel> LoadEntity(IRedirectRajalKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<RedirectRajalModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<RedirectRajalView> ListData(IIgdVisitKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());
}
