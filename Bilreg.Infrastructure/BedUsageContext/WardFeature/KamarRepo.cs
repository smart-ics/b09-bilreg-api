using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public class KamarRepo : IKamarRepo
{
    private readonly IKamarDal _kamarDal;
    public KamarRepo(IKamarDal kamarDal)
    {
        _kamarDal = kamarDal;
    }
    public void SaveChanges(KamarType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _kamarDal.Update(KamarDto.FromModel(model)),
                onNone: () => _kamarDal.Insert(KamarDto.FromModel(model)));
    }

    public MayBe<KamarType> LoadEntity(IKamarKey key)
    {   
        var dto = _kamarDal.GetData(key);
        if (dto is null)
            return MayBe<KamarType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IKamarKey key)
    {
        _kamarDal.Delete(key);
    }

    public IEnumerable<KamarType> ListData()
    {
        var listDto = _kamarDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }

    public IEnumerable<KamarType> ListData(IBangsalKey filter)
    {
        var listDto = _kamarDal.ListData()?.ToList() ?? [];
        var listFiltered = listDto
            .Where(x => x.fs_kd_bangsal == filter.BangsalId);
        var result = listFiltered.Select(x => x.ToModel()).ToList();
        return result;
    }
}