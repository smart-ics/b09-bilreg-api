using Bilreg.Application.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BhpRanapFeature;

public class BhpRanapRepo : IBhpRanapRepo
{
    private readonly IBhpRanapDal _dal;

    public BhpRanapRepo(IBhpRanapDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(BhpRanapModel model)
    {
        var dto = BhpRanapDto.FromModel(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);
    }

    public MayBe<BhpRanapModel> LoadEntity(IBhpRanapKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<BhpRanapModel>.None : MayBe.From(dto.ToModel());
    }

    public IEnumerable<BhpRanapView> ListData(IPakaiBed filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());

    public IEnumerable<BhpRanapView> ListData(IRegKey filter)
        => (_dal.ListData(filter)?.ToList() ?? []).Select(x => x.ToView());
}
