using Farinv.Application.BrgContext.KlasifikasiFeature;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.PatternHelper;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public class BentukRepo : IBentukRepo
{
    private readonly IBentukDal _bentukDal;

    public BentukRepo(IBentukDal bentukDal)
    {
        _bentukDal = bentukDal;
    }

    public void SaveChanges(BentukType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _bentukDal.Update(BentukDto.FromModel(model)),
                onNone: () => _bentukDal.Insert(BentukDto.FromModel(model)));
    }

    public MayBe<BentukType> LoadEntity(IBentukKey key)
    {
        var dto = _bentukDal.GetData(key);
        if (dto is null)
            return MayBe<BentukType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IBentukKey key)
    {
        _bentukDal.Delete(key);
    }

    public IEnumerable<BentukType> ListData()
    {
        var listDto = _bentukDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}