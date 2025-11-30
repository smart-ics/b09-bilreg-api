using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public class BedRepo : IBedRepo
{
    private readonly IBedDal _bedDal;
    public BedRepo(IBedDal bedDal)
    {
        _bedDal = bedDal;
    }
    public void SaveChanges(BedType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _bedDal.Update(BedDto.FromModel(model)),
                onNone: () => _bedDal.Insert(BedDto.FromModel(model)));
    }

    public MayBe<BedType> LoadEntity(IBedKey key)
    {   
        var dto = _bedDal.GetData(key);
        if (dto is null)
            return MayBe<BedType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IBedKey key)
    {
        _bedDal.Delete(key);
    }

    public IEnumerable<BedType> ListData()
    {
        var listDto = _bedDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }

    public IEnumerable<BedType> ListData(IBangsalKey filter)
    {
        var listDto = _bedDal.ListData()?.ToList() ?? [];
        var listFiltered = listDto
            .Where(x => x.fs_kd_bangsal == filter.BangsalId);
        var result = listFiltered.Select(x => x.ToModel()).ToList();
        return result;
    }
}