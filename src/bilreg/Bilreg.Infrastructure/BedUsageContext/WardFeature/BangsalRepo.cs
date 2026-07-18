using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public class BangsalRepo : IBangsalRepo
{
    private readonly IBangsalDal _bangsalDal;
    public BangsalRepo(IBangsalDal bangsalDal)
    {
        _bangsalDal = bangsalDal;
    }
    public void SaveChanges(BangsalType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _bangsalDal.Update(BangsalDto.FromModel(model)),
                onNone: () => _bangsalDal.Insert(BangsalDto.FromModel(model)));
    }

    public MayBe<BangsalType> LoadEntity(IBangsalKey key)
    {   
        var dto = _bangsalDal.GetData(key);
        if (dto is null)
            return MayBe<BangsalType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IBangsalKey key)
    {
        _bangsalDal.Delete(key);
    }

    public IEnumerable<BangsalType> ListData()
    {
        var listDto = _bangsalDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }

    public IEnumerable<BangsalType> ListData(ILayananKey filter)
    {
        var listDto = _bangsalDal.ListData()?.ToList() ?? [];
        var result = listDto
            .Select(x => x.ToModel())
            .Where(x => x.Layanan.LayananId == filter.LayananId)
            .ToList();
        return result;
    }
}
