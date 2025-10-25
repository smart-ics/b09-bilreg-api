using Bilreg.Application.AdmisiContext.LayananFeature.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature.LayananAgg;

public class LayananRepo : ILayananRepo
{
    private readonly ILayananDal _layananDal;

    public LayananRepo(ILayananDal layananDal)
    {
        _layananDal = layananDal;
    }

    public void SaveChanges(LayananType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _layananDal.Update(LayananDto.FromModel(model)),
                onNone: () => _layananDal.Insert(LayananDto.FromModel(model))
            );
    }

    public MayBe<LayananType> LoadEntity(ILayananKey key)
    {
        var dto = _layananDal.GetData(key);
        var model = dto?.ToModel();
        return MayBe.From(model!);
    }

    public void Delete(ILayananKey key)
    {
        _layananDal.Delete(key);
    }

    public IEnumerable<LayananType> ListData()
    {
        var listDto = _layananDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel());
        return result;
    }
}