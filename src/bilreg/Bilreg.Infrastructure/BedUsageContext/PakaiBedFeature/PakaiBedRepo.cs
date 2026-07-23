using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public class PakaiBedRepo : IPakaiBedRepo
{
    private readonly IPakaiBedDal _dal;

    public PakaiBedRepo(IPakaiBedDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(PakaiBedModel model)
    {
        var dto = PakaiBedDto.FromModel(model);
        LoadEntity(model)
            .Match(
                onSome: _ => _dal.Update(dto),
                onNone: () => _dal.Insert(dto));
    }

    public MayBe<PakaiBedModel> LoadEntity(IPakaiBed key)
    {
        var dto = _dal.GetData(key);
        return dto is null
            ? MayBe<PakaiBedModel>.None
            : MayBe.From(dto.ToModel());
    }

    public void DeleteEntity(IPakaiBed key)
        => _dal.Delete(key);

    public IEnumerable<PakaiBedModel> ListData(IRegKey filter)
        => (_dal.ListData(filter)?.ToList() ?? [])
            .Select(x => x.ToModel());
}
